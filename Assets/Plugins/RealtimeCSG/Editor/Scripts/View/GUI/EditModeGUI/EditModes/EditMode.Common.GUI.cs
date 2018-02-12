﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Helpers;
using RealtimeCSG;

namespace InternalRealtimeCSG
{
	internal sealed partial class EditModeCommonGUI
	{
		public static void UpdateButtons(CSGModel[] models)
		{
			CSG_GUIStyleUtility.InitStyles();

			bool needLightmapUVUpdate = false;
			bool needColliderUpdate = false;
			for (int m = 0; m < models.Length; m++)
			{
				if (!models[m])
					continue;

				needLightmapUVUpdate = needLightmapUVUpdate || MeshInstanceManager.NeedToGenerateLightmapUVsForModel(models[m]);
				needColliderUpdate = needColliderUpdate || MeshInstanceManager.NeedToGenerateCollidersForModel(models[m]);
			}

			if (needLightmapUVUpdate)
			{
				if (GUILayout.Button(UpdateLightmapUVContent, CSG_GUIStyleUtility.redButton))
				{
					CSGModelManager.BuildLightmapUvs();
				}
				GUILayout.Space(10);
			}

			if (needColliderUpdate)
			{
				if (GUILayout.Button(UpdateCollidersContent))
				{
					CSGModelManager.BuildColliders();
				}
				GUILayout.Space(10);
			}
		}

		public static void OnSurfaceFlagButtons(SelectedBrushSurface[] selectedBrushSurfaces, bool isSceneGUI = false)
		{
			var leftStyle		= isSceneGUI ? EditorStyles.miniButtonLeft  : GUI.skin.button;
			//var middleStyle	= isSceneGUI ? EditorStyles.miniButtonMid   : GUI.skin.button;
			var rightStyle		= isSceneGUI ? EditorStyles.miniButtonRight : GUI.skin.button;
			
			bool? noRender			= false;
			bool? noCollision		= false;
			bool? noCastShadows		= false;
			bool? noReceiveShadows	= false;
			
			if (selectedBrushSurfaces.Length > 0)
			{
				for (var i = 0; i < selectedBrushSurfaces.Length; i++)
				{
					var brush			= selectedBrushSurfaces[i].brush;
					var surfaceIndex	= selectedBrushSurfaces[i].surfaceIndex;
					if (surfaceIndex >= brush.Shape.Surfaces.Length)
					{
						Debug.LogWarning("surface_index >= brush.Shape.Surfaces.Length");
						continue; 
					}
					var texGenIndex	= brush.Shape.Surfaces[surfaceIndex].TexGenIndex;
					if (texGenIndex >= brush.Shape.TexGens.Length)
					{
						Debug.LogWarning("texGen_index >= brush.Shape.TexGens.Length");
						continue;
					}

					var texGenFlags				= brush.Shape.TexGenFlags[texGenIndex];
					var surfaceNoRender			= ((texGenFlags & TexGenFlags.NoRender)         == TexGenFlags.NoRender);
					var surfaceNoCollision		= ((texGenFlags & TexGenFlags.NoCollision)      == TexGenFlags.NoCollision);
					var surfaceNoCastShadows	= ((texGenFlags & TexGenFlags.NoCastShadows)    == TexGenFlags.NoCastShadows);
					var surfaceNoReceiveShadows	= ((texGenFlags & TexGenFlags.NoReceiveShadows) == TexGenFlags.NoReceiveShadows);

					if (i == 0)
					{
						noRender			= surfaceNoRender;
						noCollision			= surfaceNoCollision;
						noCastShadows		= surfaceNoCastShadows;
						noReceiveShadows	= surfaceNoReceiveShadows;
					} else
					{
						if (noRender		.HasValue && noRender		 .Value != surfaceNoRender		  ) noRender		 = surfaceNoRender;
						if (noCollision		.HasValue && noCollision	 .Value != surfaceNoCollision	  ) noCollision		 = surfaceNoCollision;
						if (noCastShadows   .HasValue && noCastShadows   .Value != surfaceNoCastShadows   ) noCastShadows    = surfaceNoCastShadows;
						if (noReceiveShadows.HasValue && noReceiveShadows.Value != surfaceNoReceiveShadows) noReceiveShadows = surfaceNoReceiveShadows;
					}
				}
			}
		
			GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
			{
				if (!isSceneGUI)
					GUILayout.Label(ContentShadows, EditModeSurfaceGUI.largeLabelWidth);
				else
					GUILayout.Label(ContentShadows, EditorStyles.miniLabel, EditModeSurfaceGUI.smallLabelWidth);

				EditorGUI.BeginChangeCheck();
				{
					// TODO: implement support
					EditorGUI.showMixedValue = !noReceiveShadows.HasValue;
					noReceiveShadows = !GUILayout.Toggle(!(noReceiveShadows ?? (noRender ?? true)), ContentReceiveShadowsSurfaces, leftStyle);
					TooltipUtility.SetToolTip(ToolTipReceiveShadowsSurfaces);
				}
				if (EditorGUI.EndChangeCheck())
					SurfaceUtility.SetSurfaceTexGenFlags(selectedBrushSurfaces, TexGenFlags.NoReceiveShadows, noReceiveShadows.Value);
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginChangeCheck();
				{
					// TODO: implement support
					EditorGUI.showMixedValue = !noCastShadows.HasValue;
					noCastShadows = !GUILayout.Toggle(!(noCastShadows ?? true), ContentCastShadowsSurfaces, rightStyle);
					TooltipUtility.SetToolTip(ToolTipCastShadowsSurfaces);
				}
				if (EditorGUI.EndChangeCheck())
					SurfaceUtility.SetSurfaceTexGenFlags(selectedBrushSurfaces, TexGenFlags.NoCastShadows, noCastShadows.Value);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
			{
				EditorGUI.BeginChangeCheck();
				{
					EditorGUI.showMixedValue = !noRender.HasValue;
					noRender = !GUILayout.Toggle(!(noRender ?? true), ContentVisibleSurfaces, leftStyle);
					TooltipUtility.SetToolTip(ToolTipVisibleSurfaces);
				}
				if (EditorGUI.EndChangeCheck())
					SurfaceUtility.SetSurfaceTexGenFlags(selectedBrushSurfaces, TexGenFlags.NoRender, noRender.Value);
				EditorGUI.BeginChangeCheck();
				{
					EditorGUI.showMixedValue = !noCollision.HasValue;
					noCollision = !GUILayout.Toggle(!(noCollision ?? true), ContentCollisionSurfaces, rightStyle);
					TooltipUtility.SetToolTip(ToolTipCollisionSurfaces);
				}
				if (EditorGUI.EndChangeCheck())
					SurfaceUtility.SetSurfaceTexGenFlags(selectedBrushSurfaces, TexGenFlags.NoCollision, noCollision.Value);
			}
			GUILayout.EndHorizontal();
			EditorGUI.showMixedValue = false;
		}

		public static TexGenFlags OnSurfaceFlagButtons(TexGenFlags texGenFlags, bool isSceneGUI = false)
		{
			var leftStyle	= EditorStyles.miniButtonLeft;
			//var middleStyle = EditorStyles.miniButtonMid;
			var rightStyle	= EditorStyles.miniButtonRight;

			var	noRender			= (texGenFlags & TexGenFlags.NoRender) == TexGenFlags.NoRender;
			var noCollision			= (texGenFlags & TexGenFlags.NoCollision) == TexGenFlags.NoCollision;
			var noCastShadows		= (texGenFlags & TexGenFlags.NoCastShadows) == TexGenFlags.NoCastShadows;
			var noReceiveShadows	= noRender || (texGenFlags & TexGenFlags.NoReceiveShadows) == TexGenFlags.NoReceiveShadows;

			GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
			{
				GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
				{
					GUILayout.Label(ContentShadows, EditorStyles.miniLabel, EditModeSurfaceGUI.smallLabelWidth);
					EditorGUI.BeginChangeCheck();
					{
						// TODO: implement support
						noReceiveShadows = !GUILayout.Toggle(!noReceiveShadows, ContentReceiveShadowsSurfaces, leftStyle);
						TooltipUtility.SetToolTip(ToolTipVisibleSurfaces);
					}
					if (EditorGUI.EndChangeCheck())
					{
						if (noReceiveShadows) texGenFlags |=  TexGenFlags.NoReceiveShadows; 
						else				  texGenFlags &= ~TexGenFlags.NoReceiveShadows;
						GUI.changed = true;
					}
					EditorGUI.BeginChangeCheck();
					{
						// TODO: implement support
						noCastShadows = !GUILayout.Toggle(!noCastShadows, ContentCastShadowsSurfaces, rightStyle);
						TooltipUtility.SetToolTip(ToolTipVisibleSurfaces);
					}
					if (EditorGUI.EndChangeCheck())
					{
						if (noCastShadows) texGenFlags |=  TexGenFlags.NoCastShadows; 
						else			   texGenFlags &= ~TexGenFlags.NoCastShadows;
						GUI.changed = true;
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
				{
					EditorGUI.BeginChangeCheck();
					{
						noRender = !GUILayout.Toggle(!noRender, ContentVisibleSurfaces, leftStyle);
						TooltipUtility.SetToolTip(ToolTipVisibleSurfaces);
					}
					if (EditorGUI.EndChangeCheck())
					{
						if (noRender) texGenFlags |=  TexGenFlags.NoRender; 
						else		  texGenFlags &= ~TexGenFlags.NoRender;
						GUI.changed = true;
					}
					EditorGUI.BeginChangeCheck();
					{
						noCollision = !GUILayout.Toggle(!noCollision, ContentCollisionSurfaces, rightStyle);
						TooltipUtility.SetToolTip(ToolTipVisibleSurfaces);
					}
					if (EditorGUI.EndChangeCheck())
					{
						if (noCollision) texGenFlags |=  TexGenFlags.NoCollision; 
						else		     texGenFlags &= ~TexGenFlags.NoCollision;
						GUI.changed = true;
					}
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
			return texGenFlags;
		}


		public static void StartToolGUI()
		{
			UpdateButtons(InternalCSGModelManager.Models);
		}
	}
}