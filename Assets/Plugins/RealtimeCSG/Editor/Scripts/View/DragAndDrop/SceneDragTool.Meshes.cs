﻿using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal sealed class SceneDragToolMeshDragOnSurface : ISceneDragTool
	{
		SelectedBrushSurface		hoverBrushSurface			= null;
		Vector3						hoverPosition;
		Quaternion					hoverRotation;
		Transform					hoverParent;
		int							hoverSiblingIndex;
		List<GameObject>			dragGameObjects				= null;
		List<GameObject>			visualDragGameObject		= null;
		Vector3[]					projectedBounds				= null;
		bool						haveNoParent				= false;
		PrefabSourceAlignment		sourceSurfaceAlignment		= PrefabSourceAlignment.AlignedFront;
		PrefabDestinationAlignment	destinationSurfaceAlignment	= PrefabDestinationAlignment.AlignToSurface;
		
		Vector3		prevForcedGridCenter	= MathConstants.zeroVector3;
		Quaternion	prevForcedGridRotation	= MathConstants.identityQuaternion;

		#region ValidateDrop
		public bool ValidateDrop(bool inSceneView)
		{
			if (!inSceneView)
				return false;

			Reset();
			if (DragAndDrop.objectReferences == null ||
				DragAndDrop.objectReferences.Length == 0)
			{
				dragGameObjects = null;
				return false;
			}

			bool found = false;
			dragGameObjects = new List<GameObject>();
			foreach (var obj in DragAndDrop.objectReferences)
			{
				var gameObject = obj as GameObject;
				if (gameObject == null)
					continue;

				if (PrefabUtility.GetPrefabType(gameObject) == PrefabType.None)
					continue;

				if (PrefabUtility.GetPrefabParent(gameObject) == null && 
					PrefabUtility.GetPrefabObject(gameObject) != null)
					dragGameObjects.Add(gameObject);

				if (gameObject.GetComponentInChildren<Renderer>() != null)
					found = true;
			}
			if (!found || dragGameObjects.Count != 1)
			{
				dragGameObjects = null;
				return false;
			}

			var dragGameObjectBounds = new AABB();
			dragGameObjectBounds.Reset();
			foreach (var gameObject in dragGameObjects)
			{
				var renderers = gameObject.GetComponentsInChildren<Renderer>();
				if (renderers.Length == 0)
					continue;
				foreach(var renderer in renderers)
				{
					var bounds = renderer.bounds;
					dragGameObjectBounds.Extend(bounds.min);
					dragGameObjectBounds.Extend(bounds.max);
				}
			}

			if (!dragGameObjectBounds.Valid)
				dragGameObjectBounds.Extend(MathConstants.zeroVector3);
						
			projectedBounds = new Vector3[8];
			BoundsUtilities.GetBoundsCornerPoints(dragGameObjectBounds, projectedBounds);
			/*
			var upPlane = new Plane(MathConstants.upVector3, MathConstants.zeroVector3);
			for (int i = 7; i >= 0; i--)
			{
				projectedBounds[i] = upPlane.Project(projectedBounds[i]);
				for (int j = i+1; j < projectedBounds.Length; j++)
				{
					if (projectedBounds[i] == projectedBounds[j])
					{
						ArrayUtility.RemoveAt(ref projectedBounds, j);
						break;
					}
				}
			}*/

			haveNoParent = false;
			return true;
		}
		#endregion

		#region ValidateDropPoint
		public bool ValidateDropPoint(bool inSceneView)
		{
			return true;
		}
		#endregion

		#region Reset
		public void Reset()
		{
			CleanUp();
			hoverBrushSurface	= null;
			dragGameObjects		= null;

			hoverPosition = MathConstants.zeroVector3;
			hoverRotation = MathConstants.identityQuaternion;
			hoverParent = null;
			hoverSiblingIndex = int.MaxValue;
		}
		#endregion

		void CleanUp()
		{
			if (visualDragGameObject != null)
			{
				for (int i = visualDragGameObject.Count - 1; i >= 0; i--)
				{
					if (!visualDragGameObject[i])
						continue;
					GameObject.DestroyImmediate(visualDragGameObject[i]);
				}
			}
			visualDragGameObject = null;
		}

		void DisableVisualObjects()
		{
			if (visualDragGameObject != null)
			{
				for (int i = visualDragGameObject.Count - 1; i >= 0; i--)
				{
					var obj = visualDragGameObject[i];
					if (obj == null || !obj)
						continue;
					if (obj.activeSelf)
					{
						obj.SetActive(false);
					}
				}
			}
		}

		void EnableVisualObjects()
		{
			if (visualDragGameObject == null ||
				visualDragGameObject.Count != dragGameObjects.Count)
			{
				CreateVisualObjects();
			} else
			{
				for (int i = 0; i < dragGameObjects.Count; i++)
				{
					if (visualDragGameObject[i] == null || !visualDragGameObject[i])
						continue;
					visualDragGameObject[i].SetActive(dragGameObjects[i].activeSelf);
				}
			}

			int counter = 0;
			foreach (var obj in visualDragGameObject)
			{
				if (obj == null || !obj)
					continue;
				obj.transform.rotation = hoverRotation;
				obj.transform.position = hoverPosition;
				if (hoverParent != null && !SelectionUtility.IsPrefab(hoverParent.gameObject))
					obj.transform.SetParent(hoverParent, true);
				obj.transform.SetSiblingIndex(hoverSiblingIndex + counter);
				counter++;
			}
		}
		
		void CreateVisualObjects()
		{
			CleanUp();
			
			prevForcedGridCenter = RealtimeCSG.CSGGrid.ForcedGridCenter;
			prevForcedGridRotation = RealtimeCSG.CSGGrid.ForcedGridRotation;

			sourceSurfaceAlignment = PrefabSourceAlignment.AlignedFront;
			destinationSurfaceAlignment = PrefabDestinationAlignment.Default;

			
			visualDragGameObject = new List<GameObject>();

			foreach (var obj in dragGameObjects)
			{
				var copy = PrefabUtility.InstantiatePrefab(obj) as GameObject;
				if (!copy)
					continue;

				copy.name = obj.name;
				visualDragGameObject.Add(copy);
			}
		}

		#region DragUpdated
		public bool DragUpdated(Transform transformInInspector, Rect selectionRect)
		{
			try
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

				hoverBrushSurface	= null;
				hoverParent			= (transformInInspector == null) ? null : transformInInspector.parent;
				hoverSiblingIndex	= (transformInInspector == null) ? int.MaxValue : transformInInspector.transform.GetSiblingIndex();

				float middle = (selectionRect.yMax + selectionRect.yMin) * 0.5f;
				if (Event.current.mousePosition.y > middle)
					hoverSiblingIndex++;

				hoverRotation = MathConstants.identityQuaternion;
				hoverPosition = MathConstants.zeroVector3;
				haveNoParent = true;
				return true;
			}
			finally
			{
				if (!UpdateLoop.IsActive())
					UpdateLoop.ResetUpdateRoutine();
			}
		}

		public bool DragUpdated()
		{
			try
			{
				DisableVisualObjects();
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				
				var intersection		= SceneQueryUtility.FindMeshIntersection(Event.current.mousePosition);
				var normal				= intersection.plane.normal;

				hoverPosition			= intersection.worldIntersection;
				hoverParent				= SelectionUtility.FindParentToAssignTo(intersection);
				hoverBrushSurface		= intersection.brush != null ? new SelectedBrushSurface(intersection.brush, intersection.surfaceIndex) : null;
				hoverRotation			= SelectionUtility.FindDragOrientation(normal, sourceSurfaceAlignment, destinationSurfaceAlignment);
				haveNoParent			= (hoverParent == null);
				hoverSiblingIndex		= int.MaxValue;

				RealtimeCSG.CSGGrid.SetForcedGrid(intersection.plane);

				var toggleSnapping	= (Event.current.modifiers & EventModifiers.Control) == EventModifiers.Control;
				var doSnapping		= RealtimeCSG.CSGSettings.SnapToGrid ^ toggleSnapping;
				if (doSnapping)
				{
					var localPoints = new Vector3[8];
					var localPlane	= intersection.plane;
					for (var i = 0; i < localPoints.Length; i++)
						localPoints[i] = GeometryUtility.ProjectPointOnPlane(localPlane, (hoverRotation * projectedBounds[i]) + hoverPosition);

					hoverPosition += RealtimeCSG.CSGGrid.SnapDeltaToGrid(MathConstants.zeroVector3, localPoints);
				}
				hoverPosition	= GeometryUtility.ProjectPointOnPlane(intersection.plane, hoverPosition) + (normal * 0.01f);

				EnableVisualObjects();
				return true;
			}
			finally
			{
				if (!UpdateLoop.IsActive())
					UpdateLoop.ResetUpdateRoutine();
			}
		}
		#endregion
		
		#region DragPerform
		public void DragPerform(bool inSceneView)
		{
			try
			{
				if (haveNoParent)
				{
					if (visualDragGameObject == null)
					{
						CreateVisualObjects();
					}
				}

				if (visualDragGameObject != null)
				{
					var selection = new List<GameObject>();
					for (int i = visualDragGameObject.Count - 1; i >= 0; i--)
					{
						if (!visualDragGameObject[i])
							continue;
						if (visualDragGameObject[i].activeSelf)
						{
							Undo.RegisterCreatedObjectUndo(visualDragGameObject[i], "Instantiated prefab");
							selection.Add(visualDragGameObject[i]);
						} else
						{
							GameObject.DestroyImmediate(visualDragGameObject[i]);
						}
					}
					visualDragGameObject = null;

					if (selection.Count > 0)
					{
						UnityEditor.Selection.objects = selection.ToArray();
					}
				}

				for (int i = 0; i < SceneView.sceneViews.Count; i++)
				{
					var sceneview = SceneView.sceneViews[i] as SceneView;
					if (sceneview == null)
						continue;

					if (sceneview.camera.pixelRect.Contains(Event.current.mousePosition))
						sceneview.Focus();
				}
				visualDragGameObject = null;
			}
			finally
			{
				RealtimeCSG.CSGGrid.ForcedGridCenter	= prevForcedGridCenter;
				RealtimeCSG.CSGGrid.ForcedGridRotation = prevForcedGridRotation;
				RealtimeCSG.CSGGrid.ForceGrid			= false;
			}
		}
		#endregion

		#region DragExited
		public void DragExited(bool inSceneView)
		{
			try
			{
				CleanUp();
				HandleUtility.Repaint();
			}
			finally
			{
				RealtimeCSG.CSGGrid.ForcedGridCenter	= prevForcedGridCenter;
				RealtimeCSG.CSGGrid.ForcedGridRotation = prevForcedGridRotation;
				RealtimeCSG.CSGGrid.ForceGrid			= false;
			}
		}
		#endregion

		#region Paint
		public void OnPaint()
		{
			RealtimeCSG.CSGGrid.RenderGrid();
			if (hoverBrushSurface == null)
				return;
			
			var brush = hoverBrushSurface.brush;
			var brush_cache = InternalCSGModelManager.GetBrushCache(brush);
			if (brush_cache == null ||
				brush_cache.childData == null ||
				brush_cache.childData.ModelTransform == null)
				return;
				
			var highlight_surface	= hoverBrushSurface.surfaceIndex;
			var highlight_texGen	= brush.Shape.Surfaces[highlight_surface].TexGenIndex;
			var model_translation	= brush_cache.childData.Model.transform.position;
			var brush_translation	= brush_cache.compareTransformation.modelLocalPosition + model_translation;
			CSGRenderer.DrawSelectedBrush(brush.brushID, brush.Shape, 
											brush_translation, ColorSettings.WireframeOutline, 
											highlight_texGen, 
											false, ToolConstants.oldLineScale);
		}
		#endregion
	}
}
