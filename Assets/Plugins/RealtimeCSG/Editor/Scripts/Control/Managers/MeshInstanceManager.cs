//#define SHOW_GENERATED_MESHES
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEditor;
using RealtimeCSG;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

// TODO:
// v make sure outlines are updated when models move
// v make sure it works with all special surfaces
// v    make sure all special surfaces have proper names?
// v    make sure meshrenderers are not disabled/gameobjects made inactive
// v make sure it generates one mesh when model is a trigger etc.
// v make sure it works with building scene
// v make sure it works with exporting scene
// v make sure all hideflags on all object types are set consistently
// v make sure GeneratedMeshInstance/GeneratedMeshes are only shown when SHOW_GENERATED_MESHES is set
// v make sure everything works with multiple scenes
// v make sure we remove to old scene mesh container

namespace InternalRealtimeCSG
{
	internal sealed partial class MeshInstanceManager
	{
#if SHOW_GENERATED_MESHES
		public const HideFlags ComponentHideFlags = HideFlags.DontSaveInBuild;
#else
		public const HideFlags ComponentHideFlags = HideFlags.None
													//| HideFlags.NotEditable // when this is put into a prefab (when making a prefab containing a model for instance) this will make it impossible to delete 
													| HideFlags.HideInInspector
													| HideFlags.HideInHierarchy
													| HideFlags.DontSaveInBuild
			;
#endif

		internal const string MeshContainerName			= "[generated-meshes]";
		private const string RenderMeshInstanceName		= "[generated-render-mesh]";
		private const string ColliderMeshInstanceName	= "[generated-collider-mesh]";
		private const string HelperMeshInstanceName		= "[generated-helper-mesh]";

		public static void Shutdown()
		{
		}

		public static void OnDestroyed(GeneratedMeshes container)
		{
		}

		public static void Destroy(GeneratedMeshes container)
		{
			if (container)
			{
				container.gameObject.hideFlags = HideFlags.None;
				GameObject.DestroyImmediate(container.gameObject);
			}
			// Undo.DestroyObjectImmediate // NOTE: why was this used before?
			// can't use Undo variant here because it'll mark scenes as dirty on load ..
		}

		public static void OnCreated(GeneratedMeshes container)
		{
			InitContainer(container);
		}

		public static void OnEnable(GeneratedMeshes container)
		{
			if (container)
				container.gameObject.SetActive(true);
		}

		public static void OnDisable(GeneratedMeshes container)
		{
			if (container)
				container.gameObject.SetActive(false);
		}
		
		public static void OnCreated(GeneratedMeshInstance meshInstance)
		{
			var parent = meshInstance.transform.parent;
			GeneratedMeshes container = null;
			if (parent)
				container = parent.GetComponent<GeneratedMeshes>();
			if (!container)
			{
				meshInstance.gameObject.hideFlags = HideFlags.None;
				UnityEngine.Object.DestroyImmediate(meshInstance.gameObject);
				return;
			}

			EnsureValidHelperMaterials(container.owner, meshInstance);

			var key = meshInstance.GenerateKey();
			if (!container.meshInstances.ContainsKey(key))
			{
				container.meshInstances.Add(key, meshInstance);
				return;
			}

			if (container.meshInstances[key] != meshInstance)
			{
				if (meshInstance && meshInstance.gameObject)
				{
					meshInstance.gameObject.hideFlags = HideFlags.None;
					UnityEngine.Object.DestroyImmediate(meshInstance.gameObject);
				}
			}
		}

		private static void InitContainer(GeneratedMeshes container)
		{
			if (!container)
				return;
			
			ValidateContainer(container);
		}

		public static void EnsureInitialized(GeneratedMeshes container)
		{
			InitContainer(container);
		}

		public static bool ValidateModel(CSGModel model, bool checkChildren = false)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return true;

			if (!model)
				return false;

			var modelCache = InternalCSGModelManager.GetModelCache(model);
			if (modelCache == null)
				return false;

			if (!checkChildren && modelCache.GeneratedMeshes)
				return true;

			modelCache.GeneratedMeshes = null;
			var meshContainers = model.GetComponentsInChildren<GeneratedMeshes>(true);
			if (meshContainers.Length > 1)
			{
				for (var i = meshContainers.Length - 1; i >= 0; i--)
				{
					var parentModel = meshContainers[i].GetComponentInParent<CSGModel>();
					if (parentModel != model)
					{
						ArrayUtility.RemoveAt(ref meshContainers, i);
					}
				}
			}
			if (meshContainers.Length > 1)
			{
				modelCache.GeneratedMeshes = null;
				// destroy all other meshcontainers ..
				for (var i = meshContainers.Length - 1; i >= 0; i--)
				{
					var gameObject = meshContainers[i].gameObject;
					gameObject.hideFlags = HideFlags.None;
					UnityEngine.Object.DestroyImmediate(gameObject);
				}
			}
			
			GeneratedMeshes meshContainer;
			if (meshContainers.Length >= 1)
			{
				meshContainer = meshContainers[0];
				meshContainer.owner = model;

				ValidateContainer(meshContainer);
			} else
			{
				// create it if it doesn't exist
				var containerObject = new GameObject { name = MeshContainerName };
				meshContainer = containerObject.AddComponent<GeneratedMeshes>();
				meshContainer.owner = model;
				var containerObjectTransform = containerObject.transform;
				containerObjectTransform.SetParent(model.transform, false);
				UpdateGeneratedMeshesVisibility(meshContainer, model.ShowGeneratedMeshes);
			}

			if (meshContainer)
			{
				if (meshContainer.enabled == false)
					meshContainer.enabled = true;
				var meshContainerGameObject = meshContainer.gameObject;
				var activated = (model.enabled && model.gameObject.activeSelf);
				if (meshContainerGameObject.activeSelf != activated)
					meshContainerGameObject.SetActive(activated);
			}

			modelCache.GeneratedMeshes = meshContainer;
			modelCache.ForceUpdate = true;
			return true;
		}

		public static void EnsureInitialized(CSGModel component)
		{
			ValidateModel(component);
		}

		public static RenderSurfaceType GetRenderSurfaceType(CSGModel model, Material material)
		{
			var renderSurfaceType = ModelTraits.GetModelSurfaceType(model);
			if (renderSurfaceType != RenderSurfaceType.Normal)
				return renderSurfaceType;

			return MaterialUtility.GetMaterialSurfaceType(material);
		}


		private static RenderSurfaceType GetRenderSurfaceType(CSGModel model, GeneratedMeshInstance meshInstance)
		{
			return GetRenderSurfaceType(model, meshInstance.RenderMaterial);
		}

		private static bool ShouldRenderHelperSurface(GeneratedMeshInstance meshInstance)
		{
			var renderSurfaceType = meshInstance.RenderSurfaceType;
			switch (renderSurfaceType)
			{
				case RenderSurfaceType.Hidden:			return CSGSettings.ShowHiddenSurfaces;
				case RenderSurfaceType.Culled:			return CSGSettings.ShowCulledSurfaces;
				case RenderSurfaceType.Collider:		return CSGSettings.ShowColliderSurfaces;
				case RenderSurfaceType.Trigger:			return CSGSettings.ShowTriggerSurfaces;
				case RenderSurfaceType.ShadowOnly:		
				case RenderSurfaceType.CastShadows:		return CSGSettings.ShowCastShadowsSurfaces;
				case RenderSurfaceType.ReceiveShadows:	return CSGSettings.ShowReceiveShadowsSurfaces;
			}
			switch ((MeshType)meshInstance.MeshType)
			{
				case MeshType.RenderReceiveCastShadows:	return CSGSettings.ShowReceiveShadowsSurfaces || 
															   CSGSettings.ShowCastShadowsSurfaces;
				case MeshType.RenderReceiveShadows:		return CSGSettings.ShowReceiveShadowsSurfaces;
				case MeshType.RenderCastShadows:		return CSGSettings.ShowCastShadowsSurfaces;
				case MeshType.ShadowOnly:				return CSGSettings.ShowCastShadowsSurfaces ||
															   CSGSettings.ShowCulledSurfaces;
				default: return false;
			}
		}

		private static RenderSurfaceType EnsureValidHelperMaterials(CSGModel model, GeneratedMeshInstance meshInstance)
		{
			var surfaceType = !meshInstance.RenderMaterial ? meshInstance.RenderSurfaceType : 
								GetRenderSurfaceType(model, meshInstance);
			if (surfaceType != RenderSurfaceType.Normal)
				meshInstance.RenderMaterial = MaterialUtility.GetSurfaceMaterial(surfaceType);
			return surfaceType;
		}

		private static bool IsValidMeshInstance(GeneratedMeshInstance instance)
		{
			return (!instance.PhysicsMaterial || instance.PhysicsMaterial.GetInstanceID() != 0) &&
				   (!instance.RenderMaterial || instance.RenderMaterial.GetInstanceID() != 0) &&
				   (!instance.SharedMesh || instance.SharedMesh.GetInstanceID() != 0);
		}

		public static bool HasVisibleMeshRenderer(GeneratedMeshInstance meshInstance)
		{
			if (!meshInstance)
				return false;
			return meshInstance.RenderSurfaceType == RenderSurfaceType.Normal;
		}

		public static bool HasRuntimeMesh(GeneratedMeshInstance meshInstance)
		{
			if (!meshInstance)
				return false;
			return meshInstance.RenderSurfaceType != RenderSurfaceType.Culled &&
					meshInstance.RenderSurfaceType != RenderSurfaceType.Hidden;
		}

		public static void RenderHelperSurfaces(SceneView sceneView)
		{
			var allHelperSurfaces = (CSGSettings.VisibleHelperSurfaces & ~HelperSurfaceFlags.ShowVisibleSurfaces);
			if (allHelperSurfaces == (HelperSurfaceFlags)0)
			{
				CSGSettings.VisibleHelperSurfaces = CSGSettings.DefaultHelperSurfaceFlags;
				return;
			}

			var camera			= sceneView.camera;			
			var showWireframe	= RealtimeCSG.CSGSettings.IsWireframeShown(sceneView);
				
			var visibleLayers	= Tools.visibleLayers;
			var models			= InternalCSGModelManager.Models;
			for (var i = 0; i < models.Length; i++)
			{
				var model = models[i];
				if (!model)
					continue;

				if (((1 << model.gameObject.layer) & visibleLayers) == 0)
					continue;

				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null ||
					!modelCache.GeneratedMeshes)
					continue;

				var container = modelCache.GeneratedMeshes;
				if (!container.owner ||
					!container.owner.isActiveAndEnabled)
				{
					continue;
				}

				var meshInstances = container.meshInstances;
				if (meshInstances == null ||
					meshInstances.Count == 0)
				{
					InitContainer(container);
					continue;
				}

				var modelDoesNotRender = !model.IsRenderable;
				
				var matrix = container.transform.localToWorldMatrix;
				foreach (var meshInstance in meshInstances.Values)
				{
					if (!meshInstance)
					{
						modelCache.GeneratedMeshes = null;
						meshInstance.hideFlags = HideFlags.None;
						UnityEngine.Object.DestroyImmediate(meshInstance); // wtf?
						break;
					}

					if (!ShouldRenderHelperSurface(meshInstance) ||
						modelDoesNotRender)
						continue;

					var renderSurfaceType = meshInstance.RenderSurfaceType;
					if (renderSurfaceType == RenderSurfaceType.Normal)
					{
						if (modelDoesNotRender)
						{
							if (CSGSettings.ShowCulledSurfaces) renderSurfaceType = RenderSurfaceType.Culled;
						}
						switch ((MeshType)meshInstance.MeshType)
						{
							case MeshType.RenderReceiveCastShadows:
							{
								if      (CSGSettings.ShowReceiveShadowsSurfaces) renderSurfaceType = RenderSurfaceType.ReceiveShadows;
								else if (CSGSettings.ShowCastShadowsSurfaces   ) renderSurfaceType = RenderSurfaceType.CastShadows;
								break;
							}
							case MeshType.RenderReceiveShadows:
							{
								if      (CSGSettings.ShowReceiveShadowsSurfaces) renderSurfaceType = RenderSurfaceType.ReceiveShadows;
								break;
							}
							case MeshType.RenderCastShadows:
							{
								if (CSGSettings.ShowCastShadowsSurfaces) renderSurfaceType = RenderSurfaceType.CastShadows;
								break;
							}
							case MeshType.ShadowOnly:
							{
								if (CSGSettings.ShowCastShadowsSurfaces) renderSurfaceType = RenderSurfaceType.CastShadows;
								else if (CSGSettings.ShowCulledSurfaces) renderSurfaceType = RenderSurfaceType.Culled;
								break;
							}
						}
					}
					var material = MaterialUtility.GetSurfaceMaterial(renderSurfaceType);
					if (!material)
						continue;

					if (!showWireframe)
					{
						// "DrawMeshNow" so that it renders properly in all shading modes
						if (material.SetPass(0))
							Graphics.DrawMeshNow(meshInstance.SharedMesh,
													matrix);
					} else
					{
						Graphics.DrawMesh(meshInstance.SharedMesh,
										  matrix,
										  material,
										  layer: 0,
										  camera: camera,
										  submeshIndex: 0,
										  properties: null,
										  castShadows: false,
										  receiveShadows: false);
					}
				}
			}
		}

		public static UnityEngine.Object[] FindRenderers(CSGModel[] models)
		{
			var renderers = new List<UnityEngine.Object>();
			foreach (var model in models)
			{
				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null)
					continue;

				if (!modelCache.GeneratedMeshes)
					continue;

				foreach (var renderer in modelCache.GeneratedMeshes.GetComponentsInChildren<MeshRenderer>())
				{
					if (!renderer)
						continue;

					var type = MaterialUtility.GetMaterialSurfaceType(renderer.sharedMaterial);
					if (type == RenderSurfaceType.Normal)
						continue;

					renderers.Add(renderer);
				}
			}
			return renderers.ToArray();
		}

		internal static void Reset()
		{
			for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
			{
				var scene = SceneManager.GetSceneAt(sceneIndex);
				if (!scene.isLoaded)
					continue;

				var meshInstances = SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshInstance>(scene);
				for (int m = 0; m < meshInstances.Count; m++)
				{
					meshInstances[m].hideFlags = HideFlags.None;
					UnityEngine.Object.DestroyImmediate(meshInstances[m]);
				}
			}
		}

		public static GeneratedMeshInstance CreateMesh(GeneratedMeshes container, RenderSurfaceType renderSurfaceType, int subMeshIndex, int meshType, Material renderMaterial, PhysicMaterial physicsMaterial)
		{
			if (!container || !container.owner)
				return null;

			var materialObject = new GameObject();
			if (!materialObject)
				return null;

			materialObject.transform.SetParent(container.transform, false);
			materialObject.transform.localPosition = MathConstants.zeroVector3;
			materialObject.transform.localRotation = MathConstants.identityQuaternion;
			materialObject.transform.localScale = MathConstants.oneVector3;

			var containerStaticFlags = GameObjectUtility.GetStaticEditorFlags(container.owner.gameObject);
			GameObjectUtility.SetStaticEditorFlags(materialObject, containerStaticFlags);

			var instance = materialObject.AddComponent<GeneratedMeshInstance>();
			if (!instance)
				return null;

			instance.SubMeshIndex = subMeshIndex;
			instance.MeshType = meshType;
			instance.RenderMaterial = renderMaterial;
			instance.PhysicsMaterial = physicsMaterial;
			instance.RenderSurfaceType = renderSurfaceType;

			OnCreated(instance);
			Refresh(instance, container.owner);

			return instance;
		}

		internal static void ClearMesh(GeneratedMeshInstance meshInstance)
		{
			meshInstance.SharedMesh = new Mesh();
			meshInstance.SharedMesh.MarkDynamic();
		}

		public static bool NeedToGenerateLightmapUVsForModel(CSGModel model)
		{
			if (!model)
				return false;

			var modelCache = InternalCSGModelManager.GetModelCache(model);
			if (modelCache == null ||
				!modelCache.GeneratedMeshes)
				return false;

			var container = modelCache.GeneratedMeshes;
			if (!container || container.owner != model)
				return false;

			if (container.meshInstances == null)
				return false;

			var staticFlags = GameObjectUtility.GetStaticEditorFlags(container.owner.gameObject);
			if ((staticFlags & StaticEditorFlags.LightmapStatic) != StaticEditorFlags.LightmapStatic)
				return false;
				
			foreach (var pair in container.meshInstances)
			{
				var instance = pair.Value;
				if (!instance)
					continue;

				if (NeedToGenerateLightmapUVsForInstance(instance))
					return true;
			}
			return false;
		}

		public static void GenerateLightmapUVsForModel(CSGModel model)
		{
			if (!model)
				return;

			var modelCache = InternalCSGModelManager.GetModelCache(model);
			if (modelCache == null ||
				!modelCache.GeneratedMeshes)
				return;

			var container = modelCache.GeneratedMeshes;
			if (!container || !container.owner)
				return;

			if (container.meshInstances == null)
				return;

			foreach (var pair in container.meshInstances)
			{
				var instance = pair.Value;
				if (!instance)
					continue;

				GenerateLightmapUVsForInstance(instance, model);
			}
		}

		private static void GenerateLightmapUVsForInstance(GeneratedMeshInstance instance, CSGModel model)
		{
			var meshRendererComponent = instance.CachedMeshRenderer;
			if (!meshRendererComponent)
			{
				var gameObject = instance.gameObject;
				meshRendererComponent = gameObject.GetComponent<MeshRenderer>();
				instance.CachedMeshRendererSO = null;
			}

			if (!meshRendererComponent)
				return;

			meshRendererComponent.realtimeLightmapIndex = -1;
			meshRendererComponent.lightmapIndex = -1;

			var tempMesh = new Mesh
			{
				vertices	= instance.SharedMesh.vertices,
				normals		= instance.SharedMesh.normals,
				uv			= instance.SharedMesh.uv,
				tangents	= instance.SharedMesh.tangents,
				colors		= instance.SharedMesh.colors,
				triangles	= instance.SharedMesh.triangles
			};
			instance.SharedMesh = tempMesh;

			Debug.Log("Generating lightmap UVs for the mesh " + instance.name + " of " + model.name, model);
			MeshUtility.Optimize(instance.SharedMesh);
			Unwrapping.GenerateSecondaryUVSet(instance.SharedMesh);
			instance.HasUV2 = true;
		}


		public static bool NeedToGenerateCollidersForModel(CSGModel model)
		{
			if (!model)
				return false;

			var modelCache = InternalCSGModelManager.GetModelCache(model);
			if (modelCache == null ||
				!modelCache.GeneratedMeshes)
				return false;

			var container = modelCache.GeneratedMeshes;
			if (!container || container.owner != model)
				return false;

			if (container.meshInstances == null)
				return false;
				
			foreach (var pair in container.meshInstances)
			{
				var instance = pair.Value;
				if (!instance)
					continue;

				if (NeedToGenerateCollidersForInstance(model, instance))
					return true;
			}
			return false;
		}

		private static bool NeedToGenerateLightmapUVsForInstance(GeneratedMeshInstance instance)
		{
			return !instance.HasUV2 && instance.RenderSurfaceType == RenderSurfaceType.Normal;
		}

		private static bool NeedCollider(CSGModel model, GeneratedMeshInstance instance)
		{
			return ((model.HaveCollider || model.IsTrigger) &&

					(//instance.RenderSurfaceType == RenderSurfaceType.Normal ||
						instance.RenderSurfaceType == RenderSurfaceType.Collider ||
						instance.RenderSurfaceType == RenderSurfaceType.Trigger
						) &&

					// Make sure the bounds of the mesh are not empty ...
					(Mathf.Abs(instance.SharedMesh.bounds.size.x) > MathConstants.EqualityEpsilon ||
						Mathf.Abs(instance.SharedMesh.bounds.size.y) > MathConstants.EqualityEpsilon ||
						Mathf.Abs(instance.SharedMesh.bounds.size.z) > MathConstants.EqualityEpsilon));
		}

		public static bool NeedToGenerateCollidersForInstance(CSGModel model, GeneratedMeshInstance instance)
		{
			return !instance.HasCollider && NeedCollider(model, instance);
		}

		public static void UpdateCollidersForModel(CSGModel model)
		{
			if (!model)
				return;

			var modelCache = InternalCSGModelManager.GetModelCache(model);
			if (modelCache == null ||
				!modelCache.GeneratedMeshes)
				return;

			var container = modelCache.GeneratedMeshes;
			if (!container || !container.owner)
				return;

			if (container.meshInstances == null)
				return;

			foreach (var pair in container.meshInstances)
			{
				var instance = pair.Value;
				if (!instance)
					continue;

				if (!NeedCollider(model, instance))
					continue;

				UpdateColliderForInstance(instance, model);
			}
		}

		private static void UpdateColliderForInstance(GeneratedMeshInstance instance, CSGModel model)
		{
			var meshColliderComponent = instance.CachedMeshCollider;
			if (!meshColliderComponent)
			{
				var gameObject = instance.gameObject;
				meshColliderComponent = gameObject.GetComponent<MeshCollider>();
			}

			if (!meshColliderComponent)
				return;
			
			var tempMesh = new Mesh
			{
				vertices	= instance.SharedMesh.vertices,
				triangles	= instance.SharedMesh.triangles
			};
			instance.SharedMesh = tempMesh;

			Debug.Log("Updating collider for the mesh " + instance.name + " of " + model.name, model);
			meshColliderComponent.sharedMesh = instance.SharedMesh;
			instance.HasCollider = true;
		}

		static StaticEditorFlags FilterStaticEditorFlags(StaticEditorFlags oldStaticFlags, RenderSurfaceType renderSurfaceType)
		{
			var newStaticFlags = oldStaticFlags;
			var walkable =	renderSurfaceType != RenderSurfaceType.Hidden &&
							renderSurfaceType != RenderSurfaceType.ShadowOnly &&
							renderSurfaceType != RenderSurfaceType.Culled &&
							renderSurfaceType != RenderSurfaceType.Trigger;
			if (walkable)	newStaticFlags = newStaticFlags | StaticEditorFlags.NavigationStatic;
			else			newStaticFlags = newStaticFlags & ~StaticEditorFlags.NavigationStatic;
			
			if (renderSurfaceType != RenderSurfaceType.Normal &&
				renderSurfaceType != RenderSurfaceType.ShadowOnly)
				newStaticFlags = (StaticEditorFlags)0;

			return newStaticFlags;
		}


		internal static double updateMeshColliderMeshTime = 0.0;
		public static void Refresh(GeneratedMeshInstance instance, CSGModel owner, bool postProcessScene = false, bool forceUpdate = false)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			if (!instance)
				return;

			if (!instance.SharedMesh)
			{
				ClearMesh(instance);
				instance.IsDirty = true;
			}

			if (!instance.SharedMesh)
				return;


			// Update the flags
			var oldRenderSurfaceType	= instance.RenderSurfaceType;
			if (!instance.RenderMaterial)
				instance.RenderMaterial = MaterialUtility.GetSurfaceMaterial(oldRenderSurfaceType);
			instance.RenderSurfaceType	= GetRenderSurfaceType(owner, instance);
			instance.IsDirty			= instance.IsDirty || (oldRenderSurfaceType != instance.RenderSurfaceType);


			// Update the transform, if incorrect
			var gameObject = instance.gameObject; 
			if (gameObject.transform.localPosition	!= MathConstants.zeroVector3)			gameObject.transform.localPosition	= MathConstants.zeroVector3;
			if (gameObject.transform.localRotation	!= MathConstants.identityQuaternion)	gameObject.transform.localRotation	= MathConstants.identityQuaternion;
			if (gameObject.transform.localScale		!= MathConstants.oneVector3)			gameObject.transform.localScale		= MathConstants.oneVector3;


#if SHOW_GENERATED_MESHES
			var meshInstanceFlags   = HideFlags.None;
			var transformFlags      = HideFlags.None;
			var gameObjectFlags     = HideFlags.None;
#else
			var meshInstanceFlags   = HideFlags.DontSaveInBuild | HideFlags.NotEditable;
			var transformFlags      = HideFlags.HideInInspector | HideFlags.NotEditable;
			var gameObjectFlags     = HideFlags.None;
#endif

			if (gameObject.transform.hideFlags	!= transformFlags)		gameObject.transform.hideFlags	= transformFlags;
			if (gameObject.hideFlags			!= gameObjectFlags)		gameObject.hideFlags			= gameObjectFlags;
			if (instance.hideFlags				!= meshInstanceFlags)	instance.hideFlags				= meshInstanceFlags;

			
			var showVisibleSurfaces	=	instance.RenderSurfaceType != RenderSurfaceType.Normal ||
										(RealtimeCSG.CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowVisibleSurfaces) != 0;

			if (gameObject.activeSelf != showVisibleSurfaces) gameObject.SetActive(showVisibleSurfaces);
			if (!instance.enabled) instance.enabled = true;
			
			
			// Update navigation on mesh
			var oldStaticFlags = GameObjectUtility.GetStaticEditorFlags(gameObject);
			var newStaticFlags = FilterStaticEditorFlags(GameObjectUtility.GetStaticEditorFlags(owner.gameObject), instance.RenderSurfaceType);
			if (newStaticFlags != oldStaticFlags)
			{
				GameObjectUtility.SetStaticEditorFlags(gameObject, newStaticFlags);
			}

			var meshFilterComponent		= instance.CachedMeshFilter;
			var meshRendererComponent	= instance.CachedMeshRenderer;
			if (!meshRendererComponent)
			{
				meshRendererComponent = gameObject.GetComponent<MeshRenderer>();
				instance.CachedMeshRendererSO = null;
			}

			var needMeshCollider		= NeedCollider(owner, instance);
			
			

			var needMeshRenderer		= (instance.RenderSurfaceType == RenderSurfaceType.Normal ||
										   instance.RenderSurfaceType == RenderSurfaceType.ShadowOnly);
			if (needMeshRenderer)
			{
				if (!meshFilterComponent)
				{
					meshFilterComponent = gameObject.GetComponent<MeshFilter>();
					if (!meshFilterComponent)
					{
						meshFilterComponent = gameObject.AddComponent<MeshFilter>();
						instance.CachedMeshRendererSO = null;
						instance.IsDirty = true;
					}
				}

				if (!meshRendererComponent)
				{
					meshRendererComponent = gameObject.AddComponent<MeshRenderer>();
					instance.CachedMeshRendererSO = null;
					instance.IsDirty = true;
				}
				
				if (instance.RenderSurfaceType != RenderSurfaceType.ShadowOnly)
				{ 
					if (instance.ResetLighting && meshRendererComponent)
					{
						meshRendererComponent.realtimeLightmapIndex = -1;
						meshRendererComponent.lightmapIndex			= -1;
						instance.ResetUVTime = Time.realtimeSinceStartup;
						instance.ResetLighting = false;
						instance.HasUV2 = false;
					}

					if ((owner.AutoRebuildUVs || postProcessScene || forceUpdate))
					{ 
						if ((float.IsPositiveInfinity(instance.ResetUVTime) || ((Time.realtimeSinceStartup - instance.ResetUVTime) > 2.0f)) &&
							NeedToGenerateLightmapUVsForModel(owner))
						{
							GenerateLightmapUVsForModel(owner);
						}
					}
				}

				if (!postProcessScene &&
					meshFilterComponent.sharedMesh != instance.SharedMesh)
					meshFilterComponent.sharedMesh = instance.SharedMesh;


				var ownerReceiveShadows = owner.ReceiveShadows;
				var shadowCastingMode	= owner.ShadowCastingModeFlags;
				if (instance.RenderSurfaceType == RenderSurfaceType.ShadowOnly)
				{
					shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
				}


				switch ((MeshType)instance.MeshType)
				{
					case MeshType.RenderReceiveCastShadows:
					{
						break;
					}
					case MeshType.RenderReceiveShadows:
					{
						shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
						break;
					} 
					case MeshType.RenderCastShadows:
					{
						ownerReceiveShadows = false;
						break;
					}
					case MeshType.RenderOnly:
					{
						shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
						ownerReceiveShadows = false;
						break;
					}
					case MeshType.ShadowOnly:
					{
						shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
						ownerReceiveShadows = false;
						break;
					}
				}

				if (meshRendererComponent &&
					meshRendererComponent.shadowCastingMode != shadowCastingMode)
				{
					meshRendererComponent.shadowCastingMode = shadowCastingMode;
					instance.IsDirty = true;
				}

				if (meshRendererComponent &&
					meshRendererComponent.receiveShadows != ownerReceiveShadows)
				{
					meshRendererComponent.receiveShadows = ownerReceiveShadows;
					instance.IsDirty = true;
				}

				var requiredMaterial = instance.RenderMaterial;
				if (shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
					// Note: need non-transparent material here
					requiredMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

				if (!requiredMaterial)
					requiredMaterial = MaterialUtility.MissingMaterial;

				//*
				var meshRendererComponentSO	= instance.CachedMeshRendererSO as UnityEditor.SerializedObject;
				if (meshRendererComponentSO == null)
				{
					if (meshRendererComponent)
					{
						instance.CachedMeshRendererSO =
						meshRendererComponentSO = new SerializedObject(meshRendererComponent);
					}
				} else
				if (!meshRendererComponent)
				{
					instance.CachedMeshRendererSO =
					meshRendererComponentSO = null; 
				}
				if (meshRendererComponentSO != null)
				{
					meshRendererComponentSO.Update();
					var preserveUVsProperty = meshRendererComponentSO.FindProperty("m_PreserveUVs");
					var preserveUVs = owner.PreserveUVs;
					if (preserveUVsProperty.boolValue != preserveUVs)
					{
						preserveUVsProperty.boolValue = preserveUVs;
						meshRendererComponentSO.ApplyModifiedProperties();
					}
				}
				//*/

				if (meshRendererComponent &&
					meshRendererComponent.sharedMaterial != requiredMaterial)
				{
					meshRendererComponent.sharedMaterial = requiredMaterial;
					instance.IsDirty = true;
				}

				// we don't actually want the unity style of rendering a wireframe 
				// for our meshes, so we turn it off
				//*
#if UNITY_5_5_OR_NEWER && !UNITY_5_5_0
				EditorUtility.SetSelectedRenderState(meshRendererComponent, EditorSelectedRenderState.Hidden);
#else
				EditorUtility.SetSelectedWireframeHidden(meshRendererComponent, true);
#endif
				//*/
			} else
			{
				if (!meshFilterComponent)	{ meshFilterComponent = gameObject.GetComponent<MeshFilter>(); }
				if (meshFilterComponent)	{ meshFilterComponent.hideFlags = HideFlags.None; UnityEngine.Object.DestroyImmediate(meshFilterComponent); instance.IsDirty = true; }
				if (meshRendererComponent)	{ meshRendererComponent.hideFlags = HideFlags.None; UnityEngine.Object.DestroyImmediate(meshRendererComponent); instance.IsDirty = true; }
				instance.ResetLighting = false;
				meshFilterComponent = null;
				meshRendererComponent = null;
				instance.CachedMeshRendererSO = null;
			}
						
			instance.CachedMeshFilter   = meshFilterComponent;
			instance.CachedMeshRenderer = meshRendererComponent;

			// TODO:	navmesh specific mesh
			// TODO:	occludee/reflection probe static
			
			var meshColliderComponent	= instance.CachedMeshCollider;
			if (!meshColliderComponent)
				meshColliderComponent = gameObject.GetComponent<MeshCollider>();
			if (needMeshCollider)
			{
				if (meshColliderComponent && !meshColliderComponent.enabled)
					meshColliderComponent.enabled = true;

				if (!meshColliderComponent)
				{
					meshColliderComponent = gameObject.AddComponent<MeshCollider>();
					instance.IsDirty = true;
				}

				// stops it from rendering wireframe in scene
				if ((meshColliderComponent.hideFlags & HideFlags.HideInHierarchy) == 0)
					meshColliderComponent.hideFlags |= HideFlags.HideInHierarchy;

				if (meshColliderComponent.sharedMaterial != instance.PhysicsMaterial)
				{
					meshColliderComponent.sharedMaterial = instance.PhysicsMaterial;
					instance.IsDirty = true;
				}

				var setToConvex = owner.SetColliderConvex;
				if (meshColliderComponent.convex != setToConvex)
				{
					meshColliderComponent.convex = setToConvex;
					instance.IsDirty = true;
				}

				if (instance.RenderSurfaceType == RenderSurfaceType.Trigger ||
					owner.IsTrigger)
				{
					if (!meshColliderComponent.isTrigger)
					{
						meshColliderComponent.isTrigger = true;
						instance.IsDirty = true;
					}
				} else
				{
					if (meshColliderComponent.isTrigger)
					{
						meshColliderComponent.isTrigger = false;
						instance.IsDirty = true;
					}
				}

				if (instance.HasCollider &&
					meshColliderComponent.sharedMesh != instance.SharedMesh)
				{
					instance.HasCollider = false; 
					instance.ResetColliderTime = Time.realtimeSinceStartup;
				}


				if ((owner.AutoRebuildColliders || postProcessScene || forceUpdate) && 
					(
						!instance.HasCollider && ((postProcessScene || forceUpdate) ||
						(float.IsPositiveInfinity(instance.ResetColliderTime) || ((Time.realtimeSinceStartup - instance.ResetColliderTime) > 2.0f)))
					))
				{
					var startUpdateMeshColliderMeshTime = EditorApplication.timeSinceStartup;
					meshColliderComponent.sharedMesh = instance.SharedMesh;
					updateMeshColliderMeshTime += EditorApplication.timeSinceStartup - startUpdateMeshColliderMeshTime;
					instance.HasCollider = true;
				}

				// .. for some reason this fixes mesh-colliders not being found with ray-casts in the editor?
				if (instance.IsDirty)
				{
					meshColliderComponent.enabled = false;
					meshColliderComponent.enabled = true;
				}
			} else
			{
				if (meshColliderComponent) { meshColliderComponent.hideFlags = HideFlags.None; UnityEngine.Object.DestroyImmediate(meshColliderComponent); instance.IsDirty = true; }
				meshColliderComponent = null;
			}
			instance.CachedMeshCollider = meshColliderComponent;
			
			if (!postProcessScene)
			{
#if SHOW_GENERATED_MESHES
				if (instance.IsDirty)
					UpdateName(instance);
#else
				if (needMeshRenderer)
				{
					if (instance.name != RenderMeshInstanceName)
						instance.name = RenderMeshInstanceName;
				} else
				if (needMeshCollider)
				{
					if (instance.name != ColliderMeshInstanceName)
						instance.name = ColliderMeshInstanceName;
				} else
				{
					if (instance.name != HelperMeshInstanceName)
						instance.name = HelperMeshInstanceName;
				}
#endif
				instance.IsDirty = false;
			}
		}

#if SHOW_GENERATED_MESHES
		private static void UpdateName(GeneratedMeshInstance instance)
		{
			var renderMaterial			= instance.RenderMaterial;
			var parentObject			= instance.gameObject;

			var builder = new System.Text.StringBuilder();
			builder.Append(instance.RenderSurfaceType);
			builder.Append(' ');
			builder.Append(instance.GetInstanceID());

			if (instance.PhysicsMaterial)
			{
				var physicmaterialName = ((!instance.PhysicsMaterial) ? "default" : instance.PhysicsMaterial.name);
				if (builder.Length > 0) builder.Append(' ');
				builder.AppendFormat(" Physics [{0}]", physicmaterialName);
			}
			if (renderMaterial)
			{
				builder.AppendFormat(" Material [{0} {1}]", renderMaterial.name, renderMaterial.GetInstanceID());
			}

			builder.AppendFormat(" Key {0}", instance.GenerateKey().GetHashCode());

			var objectName = builder.ToString();
			if (parentObject.name != objectName) parentObject.name = objectName;
			if (instance.SharedMesh &&
				instance.SharedMesh.name != objectName)
				instance.SharedMesh.name = objectName;
		}
#endif

		static int RefreshModelCounter = 0;
		
		public static void UpdateHelperSurfaceVisibility(bool force = false)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;


			updateMeshColliderMeshTime = 0.0;
			var models = InternalCSGModelManager.Models;
			var currentRefreshModelCount = 0;
			for (var i = 0; i < models.Length; i++)
			{
				var model = models[i];
				if (!model)
					continue;

				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null ||
					!modelCache.GeneratedMeshes)
					continue;

				var container = modelCache.GeneratedMeshes;
				if (!container || !container.owner)
					continue;

				if (container.meshInstances == null)
				{
					InitContainer(container);

					if (container.meshInstances == null)
						continue;
				}

				if (!container)
					continue;

				if (force ||
					RefreshModelCounter == currentRefreshModelCount)
				{
					UpdateContainerFlags(container);
					foreach (var pair in container.meshInstances)
					{
						var instance = pair.Value;
						if (!instance)
							continue;

						Refresh(instance, container.owner);
					}
				}
				currentRefreshModelCount++;
			}
			
			if (RefreshModelCounter < currentRefreshModelCount)
				RefreshModelCounter++;
			else
				RefreshModelCounter = 0;
		}

		private static void AssignLayerToChildren(GameObject gameObject)
		{
			if (!gameObject)
				return;
			var layer = gameObject.layer;
			foreach (var transform in gameObject.GetComponentsInChildren<Transform>(true)) 
				transform.gameObject.layer = layer;
		}

		public static void UpdateGeneratedMeshesVisibility(CSGModel model)
		{
			var modelCache = InternalCSGModelManager.GetModelCache(model);
			if (modelCache == null ||
				!modelCache.GeneratedMeshes)
				return;

			UpdateGeneratedMeshesVisibility(modelCache.GeneratedMeshes, model.ShowGeneratedMeshes);
		}

		public static void UpdateGeneratedMeshesVisibility(GeneratedMeshes container, bool visible)
		{
			if (!container.owner.isActiveAndEnabled ||
				(container.owner.hideFlags & (HideFlags.HideInInspector|HideFlags.HideInHierarchy)) == (HideFlags.HideInInspector|HideFlags.HideInHierarchy))
				return;

			var containerGameObject = container.gameObject; 
			
			HideFlags newFlags;
#if SHOW_GENERATED_MESHES
			newFlags = HideFlags.None;
#else
			if (visible)
				newFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
			else
				newFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
#endif

			if (containerGameObject.hideFlags   != newFlags)
				containerGameObject.hideFlags   = newFlags;

			if (container.hideFlags != newFlags)
			{
				container.transform.hideFlags   = newFlags;
				container.hideFlags             = newFlags | ComponentHideFlags;
			}
		}

		static void AutoUpdateRigidBody(GeneratedMeshes container)
		{
			var model		= container.owner;
			var gameObject	= model.gameObject;
			if (ModelTraits.NeedsRigidBody(model))
			{
				var rigidBody = container.CachedRigidBody;
				if (!rigidBody)
					rigidBody = model.GetComponent<Rigidbody>();
				if (!rigidBody)
					rigidBody = gameObject.AddComponent<Rigidbody>();

				if (rigidBody.hideFlags != HideFlags.None)
					rigidBody.hideFlags = HideFlags.None;

				RigidbodyConstraints constraints;
				bool isKinematic;
				bool useGravity;
				if (ModelTraits.NeedsStaticRigidBody(model))
				{
					isKinematic = true;
					useGravity = false;
					constraints = RigidbodyConstraints.FreezeAll;
				} else
				{
					isKinematic = false;
					useGravity = true;
					constraints = RigidbodyConstraints.None;
				}
				
				if (rigidBody.isKinematic != isKinematic) rigidBody.isKinematic = isKinematic;
				if (rigidBody.useGravity  != useGravity) rigidBody.useGravity = useGravity;
				if (rigidBody.constraints != constraints) rigidBody.constraints = constraints;
				container.CachedRigidBody = rigidBody;
			} else
			{
				var rigidBody = container.CachedRigidBody;
				if (!rigidBody)
					rigidBody = model.GetComponent<Rigidbody>();
				if (rigidBody)
				{
					rigidBody.hideFlags = HideFlags.None;
					UnityEngine.Object.DestroyImmediate(rigidBody);
				}
				container.CachedRigidBody = null;
			}
		}

		public static void RemoveIfEmpty(GameObject gameObject)
		{
			var allComponents = gameObject.GetComponents<Component>();
			for (var i = 0; i < allComponents.Length; i++)
			{
				if (allComponents[i] is Transform)
					continue;
				if (allComponents[i] is GeneratedMeshInstance)
					continue;

				return;
			}
			gameObject.hideFlags = HideFlags.None;
			UnityEngine.Object.DestroyImmediate(gameObject);
		}
		
		public static void ValidateContainer(GeneratedMeshes meshContainer)
		{
			if (!meshContainer)
				return;

			var containerObject             = meshContainer.gameObject;
			var containerObjectTransform    = containerObject.transform;
			
			if (meshContainer.owner)
			{
				var modelCache = InternalCSGModelManager.GetModelCache(meshContainer.owner);
				if (modelCache != null)
				{
					if (modelCache.GeneratedMeshes &&
						modelCache.GeneratedMeshes != meshContainer)
					{
						meshContainer.hideFlags = HideFlags.None;
						meshContainer.gameObject.hideFlags = HideFlags.None;
						UnityEngine.Object.DestroyImmediate(meshContainer.gameObject);
						return;
					}
				}
			} else
			if (meshContainer.owner != null)
			{
				meshContainer.hideFlags = HideFlags.None;
				meshContainer.gameObject.hideFlags = HideFlags.None;
				UnityEngine.Object.DestroyImmediate(meshContainer.gameObject);
				return;
			}

			meshContainer.meshInstances.Clear();
			for (var i = 0; i < containerObjectTransform.childCount; i++)
			{
				var child = containerObjectTransform.GetChild(i);
				var meshInstance = child.GetComponent<GeneratedMeshInstance>();
				if (!meshInstance)
				{
					child.hideFlags = HideFlags.None;
					child.gameObject.hideFlags = HideFlags.None;
					UnityEngine.Object.DestroyImmediate(child.gameObject);
					continue;
				}
				var key = meshInstance.GenerateKey();
				if (meshContainer.meshInstances.ContainsKey(key))
				{
					child.hideFlags = HideFlags.None;
					child.gameObject.hideFlags = HideFlags.None;
					UnityEngine.Object.DestroyImmediate(child.gameObject);
					continue;
				}

				if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal && !meshInstance.RenderMaterial)
				{
					child.hideFlags = HideFlags.None;
					child.gameObject.hideFlags = HideFlags.None;
					UnityEngine.Object.DestroyImmediate(child.gameObject);
					continue;
				}

				if (!IsValidMeshInstance(meshInstance) && !EditorApplication.isPlayingOrWillChangePlaymode)
				{
					child.hideFlags = HideFlags.None;
					child.gameObject.hideFlags = HideFlags.None;
					UnityEngine.Object.DestroyImmediate(child.gameObject);
					continue;
				}
				
				meshContainer.meshInstances[key] = meshInstance;
			}

			if (containerObject.name != MeshContainerName)
			{
				Debug.Log("3");
				var flags = containerObject.hideFlags;
				if (containerObject.hideFlags != HideFlags.None) containerObject.hideFlags = HideFlags.None;
				containerObject.name  = MeshContainerName;
				if (containerObject.hideFlags != flags) containerObject.hideFlags = flags;
			}

			if (meshContainer.owner)
				UpdateGeneratedMeshesVisibility(meshContainer, meshContainer.owner.ShowGeneratedMeshes);
			
			if (meshContainer.owner)
			{
				var modelTransform = meshContainer.owner.transform;
				if (containerObjectTransform.parent != modelTransform)
					containerObjectTransform.parent.SetParent(modelTransform, true);
			}
		}

		public static GeneratedMeshInstance FindMesh(GeneratedMeshes container, int subMeshIndex, MeshType meshType, Material renderMaterial, PhysicMaterial physicsMaterial)
		{
			var renderSurfaceType = GetRenderSurfaceType(container.owner, renderMaterial);

			var key	= MeshInstanceKey.GenerateKey(renderSurfaceType, subMeshIndex, (int)meshType, renderMaterial, physicsMaterial);
			GeneratedMeshInstance instance;
			if (!container.meshInstances.TryGetValue(key, out instance))
				return null;

			if (instance)
				return instance;

			container.meshInstances.Remove(key);
			return null;
		}

		public static GeneratedMeshInstance[] GetAllModelMeshInstances(GeneratedMeshes container)
		{
			if (container.meshInstances == null ||
				container.meshInstances.Count == 0)
				return null;

			return container.meshInstances.Values.ToArray();
		}

		public static GeneratedMeshInstance GetMesh(GeneratedMeshes container, int subMeshIndex, MeshType meshType, Material renderMaterial, PhysicMaterial physicsMaterial)
		{
			if (container.meshInstances == null ||
				container.meshInstances.Count == 0)
			{ 
				InitContainer(container);
				if (container.meshInstances == null)
					return null;
			}

			if (!renderMaterial) renderMaterial = null;
			if (!physicsMaterial) physicsMaterial = null;

			//FixMaterials(ref renderMaterial);

			var renderSurfaceType	= MaterialUtility.GetMaterialSurfaceType(renderMaterial);

			var key					= MeshInstanceKey.GenerateKey(renderSurfaceType, subMeshIndex, (int)meshType, renderMaterial, physicsMaterial);
			GeneratedMeshInstance instance;
			if (container.meshInstances.TryGetValue(key, out instance))
			{
				if (instance)
					return instance;

				container.meshInstances.Remove(key);
			}

			var removeKeys = new List<MeshInstanceKey>();
			foreach(var pair in container.meshInstances)
			{
				var meshInstance = pair.Value;

				if (meshInstance.SubMeshIndex    != subMeshIndex ||
					meshInstance.MeshType        != (int)meshType ||
					meshInstance.RenderMaterial  != renderMaterial ||
					meshInstance.PhysicsMaterial != physicsMaterial)
					continue;

				removeKeys.Add(pair.Key);
				if (!meshInstance)
					continue;
				
				meshInstance.gameObject.hideFlags = HideFlags.None;
				UnityEngine.Object.DestroyImmediate(meshInstance.gameObject);
			}

			for (var i = 0; i < removeKeys.Count; i++)
				container.meshInstances.Remove(removeKeys[i]);

			instance = CreateMesh(container, renderSurfaceType, subMeshIndex, (int)meshType, renderMaterial, physicsMaterial);
			if (!instance)
				return null;

			EnsureValidHelperMaterials(container.owner, instance);
			container.meshInstances[key] = instance;
			return instance;
		}


		#region UpdateTransform
		public static void UpdateTransforms()
		{
			var models = InternalCSGModelManager.Models;
			for (var i = 0; i < models.Length; i++)
			{
				var model = models[i];
				if (!model)
					continue;

				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null)
					continue;

				UpdateTransform(modelCache.GeneratedMeshes);
			}
		}

		static void UpdateTransform(GeneratedMeshes container)
		{
			if (!container || !container.owner)
			{
				return;
			}

			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			// TODO: make sure outlines are updated when models move
			
			var containerTransform = container.transform;
			if (containerTransform.localPosition	!= MathConstants.zeroVector3 ||
				containerTransform.localRotation	!= MathConstants.identityQuaternion ||
				containerTransform.localScale		!= MathConstants.oneVector3)
			{
				containerTransform.localPosition	= MathConstants.zeroVector3;
				containerTransform.localRotation	= MathConstants.identityQuaternion;
				containerTransform.localScale		= MathConstants.oneVector3;
				SceneToolRenderer.SetOutlineDirty();
			}
		}
		#endregion

		#region UpdateContainerComponents
		public static void UpdateContainerComponents(GeneratedMeshes container, HashSet<GeneratedMeshInstance> foundInstances)
		{
			if (!container || !container.owner)
				return;
			
			if (container.meshInstances == null)
				InitContainer(container);


			var notfoundInstances	= new List<GeneratedMeshInstance>();

			var instances			= container.GetComponentsInChildren<GeneratedMeshInstance>(true);
			if (foundInstances == null)
			{
				notfoundInstances.AddRange(instances);
			} else
			{ 
				foreach (var instance in instances)
				{
					if (!foundInstances.Contains(instance))
					{
						notfoundInstances.Add(instance);
						continue;
					}
					
					var key = instance.GenerateKey();
					container.meshInstances[key] = instance;
				}
			}

			foreach (var instance in notfoundInstances)
			{
				if (instance && instance.gameObject)
				{
					instance.gameObject.hideFlags = HideFlags.None;
					UnityEngine.Object.DestroyImmediate(instance.gameObject);
				}
				var items = container.meshInstances.ToArray();
				foreach(var item in items)
				{
					if (!item.Value ||
						item.Value == instance)
					{
						container.meshInstances.Remove(item.Key);
					}
				}
			}
			 
			if (!container.owner)
				return;

			UpdateTransform(container);
		}
		#endregion
		
		#region UpdateContainerFlags
		private static void UpdateContainerFlags(GeneratedMeshes container)
		{
			if (!container)
				return;
			if (container.owner)
			{
				var ownerTransform = container.owner.transform;
				if (container.transform.parent != ownerTransform) 
				{
					container.transform.SetParent(ownerTransform, false);
				}

				if (!container)
					return;
			}

			//var isTrigger			= container.owner.IsTrigger;
			//var collidable		= container.owner.HaveCollider || isTrigger;
			var ownerStaticFlags	= GameObjectUtility.GetStaticEditorFlags(container.owner.gameObject);
			var previousStaticFlags	= GameObjectUtility.GetStaticEditorFlags(container.gameObject);
			var containerTag		= container.owner.gameObject.tag;
			var containerLayer		= container.owner.gameObject.layer;
			
			var showVisibleSurfaces		= (RealtimeCSG.CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowVisibleSurfaces) != 0;


			if (ownerStaticFlags != previousStaticFlags ||
				containerTag   != container.gameObject.tag ||
				containerLayer != container.gameObject.layer)
			{
				foreach (var meshInstance in container.meshInstances.Values)
				{
					//var instanceFlags = ownerStaticFlags;

					if (!meshInstance)
						continue;

					if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal)
					{
						var gameObject = meshInstance.gameObject;
						if (gameObject.activeSelf != showVisibleSurfaces)
							gameObject.SetActive(showVisibleSurfaces);
					}

					var oldStaticFlags = GameObjectUtility.GetStaticEditorFlags(meshInstance.gameObject);
					var newStaticFlags = FilterStaticEditorFlags(oldStaticFlags, meshInstance.RenderSurfaceType);

					foreach (var transform in meshInstance.GetComponentsInChildren<Transform>(true))
					{
						var gameObject = transform.gameObject;
						if (oldStaticFlags != newStaticFlags)
							GameObjectUtility.SetStaticEditorFlags(gameObject, newStaticFlags);
						if (gameObject.tag != containerTag)
							gameObject.tag = containerTag;
						if (gameObject.layer != containerLayer)
							gameObject.layer = containerLayer;
					}
				}
			}

			if (container.owner.NeedAutoUpdateRigidBody)
				AutoUpdateRigidBody(container);
		}
#endregion
	}
}
