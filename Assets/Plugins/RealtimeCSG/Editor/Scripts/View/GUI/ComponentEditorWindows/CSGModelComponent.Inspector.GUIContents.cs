using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal sealed partial class CSGModelComponentInspectorGUI
	{
		private static readonly GUIContent ExportLabel							= new GUIContent("Export");
		private static readonly GUIContent ExportOriginLabel					= new GUIContent("Origin");
		private static readonly GUIContent ExportColliderLabel                  = new GUIContent("Export Colliders");
		private static readonly GUIContent ExportToButtonLabel                  = new GUIContent("Export to ...");

		private static readonly GUIContent CastShadows							= new GUIContent("Cast Shadows", "Only opaque materials cast shadows");
		private static readonly GUIContent ReceiveShadowsContent				= new GUIContent("Receive Shadows", "Only opaque materials receive shadows (is always on in deferred mode)");
		private static readonly GUIContent GenerateColliderContent				= new GUIContent("Generate Collider");
		private static readonly GUIContent AutoRebuildCollidersContent          = new GUIContent("Auto Rebuild Colliders", "Automatically regenerate colliders when the model has been modified. This may introduce hitches when modifying geometry.");
		private static readonly GUIContent ModelIsTriggerContent				= new GUIContent("Model Is Trigger");
		private static readonly GUIContent ColliderSetToConvexContent			= new GUIContent("Convex Collider", "Set generated collider to convex");
		private static readonly GUIContent ColliderAutoRigidBodyContent			= new GUIContent("Auto RigidBody", "When enabled the model automatically updates the Rigidbody settings, creates it when needed, destroys it when not needed.");
		private static readonly GUIContent DefaultPhysicsMaterialContent		= new GUIContent("Default Physics Material");
		private static readonly GUIContent InvertedWorldContent					= new GUIContent("Inverted world", "World is solid by default when checked, otherwise default is empty");
		private static readonly GUIContent DoNotRenderContent					= new GUIContent("Do Not Render");
		
		private static readonly GUIContent AutoRebuildUVsContent				= new GUIContent("Auto Rebuild UVs", "Automatically regenerate lightmap UVs when the model has been modified. This may introduce hitches when modifying geometry.");
		private static readonly GUIContent PreserveUVsContent                   = new GUIContent("Preserve UVs", "Preserve the incoming lightmap UVs when generating realtime GI UVs. The incoming UVs are packed but charts are not scaled or merged. This is necessary for correct edge stitching of axis aligned chart edges.");
		private static readonly GUIContent ShowGeneratedMeshesContent			= new GUIContent("Show Meshes", "Select to show the generated Meshes in the hierarchy");

		private static readonly GUIContent VertexChannelColorContent			= new GUIContent("Color channel");
		private static readonly GUIContent VertexChannelTangentContent			= new GUIContent("Tangent channel");
		private static readonly GUIContent VertexChannelNormalContent			= new GUIContent("Normal channel");
		private static readonly GUIContent VertexChannelUV1Content				= new GUIContent("UV1 channel");
	}
}
 