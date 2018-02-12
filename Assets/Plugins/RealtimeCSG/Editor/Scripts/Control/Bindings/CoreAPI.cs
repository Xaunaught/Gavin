using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// WORK IN PROGRESS
#if false
namespace RealtimeCSG
{
	// TODO: - only set parent/model on the model/parent, do -not- set it explicitly on children
	//		 - remove necessity to have both node id AND operation/model/brush id

	// TODO: - add surface 
	//		 - give each surface an unique id

	// TODO: - add ability to generate meshes w/ CoreModel

	[Serializable]
	public class CoreIntersection
	{
		public Int32        modelID;

		public Int32        brushID;
		public Int32        surfaceIndex;
		public Int32        texGenIndex;
		
		public CSGPlane     plane;
		public bool         surfaceInverted;

		public Vector2      surfaceIntersection;
		public Vector3      worldIntersection;
		public float        distance;
	};

	public struct CoreModel
	{
		internal Int32 modelID;
		internal Int32 nodeID;

		public static CoreModel Create(Int32 uniqueID, string debugName) // remove necessity to have a debugname
		{
			var model = new CoreModel();
			if (!GenerateModelID(uniqueID, debugName, out model.modelID, out model.nodeID))
			{
				model.modelID = 0;
				model.nodeID = 0;
				return model;
			}
						
			return model;
		}

		public bool		Enabled			{ get { return GetModelEnabled(modelID); } set { SetModelEnabled(modelID, value); } }

		public Int32[]	ChildNodeIDs	{ get { return GetModelChildren(modelID); } set { SetModelChildren(modelID, value); } }

		public bool		IsValid			{ get { return (modelID != 0) && (nodeID != 0); } }
		public Int32	NodeID			{ get { return nodeID; } }
		public Int32	UniqueID		{ get { return GetModelUniqueID(modelID); } }

		public void		SetDirty()		{ CoreAPI.UpdateNode(nodeID); }

		public void		Destroy()		{ RemoveModel(modelID); modelID = 0; nodeID = 0; }

		public CoreIntersection[] RayCast(Vector3 rayStart, Vector3 rayEnd, bool ignoreInvisiblePolygons = true, Int32[] ignoreBrushIDs = null)
		{
			return RayCastIntoModelMulti(modelID, rayStart, rayEnd, ignoreInvisiblePolygons, ignoreBrushIDs);
		}

		#region Models C++ functions

		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GenerateModelID(Int32		uniqueID,
												   [In] string	name,
												   out Int32	generatedModelID,
												   out Int32	generatedNodeID);

		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool RemoveModel(Int32 modelID);

		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern Int32 GetModelUniqueID(Int32 modelID);


		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool GetModelEnabled(Int32 modelID);

		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool SetModelEnabled(Int32 modelID, bool isEnabled);


		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetModelChildren(Int32		modelID, 
													Int32		childCount,
													IntPtr		childrenNodeIDs);
		private static bool SetModelChildren(Int32		modelID,
											 Int32[]	childNodeIDs)
		{
			if (childNodeIDs == null)
				childNodeIDs = new Int32[0];

			GCHandle	childNodeIDsHandle	= GCHandle.Alloc(childNodeIDs, GCHandleType.Pinned);
			IntPtr		childNodeIDsPtr		= childNodeIDsHandle.AddrOfPinnedObject();
			var result = SetModelChildren(modelID,
										  childNodeIDs.Length,
										  childNodeIDsPtr);
			childNodeIDsHandle.Free();
			return result;
		}

		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern Int32 GetModelChildCount(Int32 modelID);

		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool	GetModelChildren(Int32	modelID,
													 Int32	childCount,
													 IntPtr	childNodeIDs);
		private static Int32[]  GetModelChildren(Int32 modelID)
		{
			var childCount		= GetModelChildCount(modelID);
			var childNodeIDs	= new Int32[childCount];
			if (childCount == 0)
				return childNodeIDs;

			GCHandle childNodeIDsHandle = GCHandle.Alloc(childNodeIDs, GCHandleType.Pinned);
			IntPtr childNodeIDsPtr = childNodeIDsHandle.AddrOfPinnedObject();
			GetModelChildren(modelID,
							 childCount,
							 childNodeIDsPtr);
			childNodeIDsHandle.Free();
			return childNodeIDs;
		}


		#region RayCastIntoModelMulti
		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern Int32 RayCastIntoModelMultiCount(Int32			modelID,
															   [In] ref Vector3	rayStart,
															   [In] ref Vector3	rayEnd,
															   bool				ignoreInvisiblePolygons,
															   IntPtr			ignoreNodeIndices,
															   Int32			ignoreNodeIndexCount);
		
		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RayCastIntoModelMultiGet(int			objectCount,
															IntPtr		distance,
															IntPtr		uniqueID,
															IntPtr		brushID,
															IntPtr		surfaceIndex,
															IntPtr		NativeTexGenIndex,
															IntPtr		surfaceIntersection,
															IntPtr		worldIntersection,
															IntPtr		surfaceInverted,
															IntPtr		plane);

		private static CoreIntersection[] RayCastIntoModelMulti(Int32		modelID,
																Vector3		rayStart,
																Vector3		rayEnd,
																bool		ignoreInvisiblePolygons = true,
																Int32[]		ignoreBrushIDs = null)
        {
			IntPtr ignoreNodeIndicesPtr = IntPtr.Zero;
			GCHandle ignoreNodeIndicesHandle = new GCHandle();
			
			if (ignoreBrushIDs != null)
			{
				ignoreNodeIndicesHandle = GCHandle.Alloc(ignoreBrushIDs, GCHandleType.Pinned);
				ignoreNodeIndicesPtr = ignoreNodeIndicesHandle.AddrOfPinnedObject();
			}
			
			Int32 intersectionCount = RayCastIntoModelMultiCount(modelID, 
																 ref rayStart,
																 ref rayEnd,
																 ignoreInvisiblePolygons,
																 ignoreNodeIndicesPtr,
																 (ignoreBrushIDs == null) ? 0 : ignoreBrushIDs.Length);

			if (ignoreNodeIndicesHandle.IsAllocated)
				ignoreNodeIndicesHandle.Free();

			if (intersectionCount == 0)
				return null;
			
			var distance			= new Single[intersectionCount];
			var uniqueID			= new Int32[intersectionCount];
			var brushID				= new Int32[intersectionCount];
			var surfaceIndex		= new Int32[intersectionCount];
			var texGenIndex			= new Int32[intersectionCount];
			var surfaceIntersection = new Vector2[intersectionCount];
			var worldIntersection	= new Vector3[intersectionCount];
			var surfaceInverted		= new byte[intersectionCount];
			var planes				= new CSGPlane[intersectionCount];

			
			GCHandle distanceHandle				= GCHandle.Alloc(distance, GCHandleType.Pinned);
			GCHandle uniqueIDHandle				= GCHandle.Alloc(uniqueID, GCHandleType.Pinned);
			GCHandle brushIDHandle				= GCHandle.Alloc(brushID, GCHandleType.Pinned);
			GCHandle surfaceIndexHandle			= GCHandle.Alloc(surfaceIndex, GCHandleType.Pinned);
			GCHandle texGenIndexHandle			= GCHandle.Alloc(texGenIndex, GCHandleType.Pinned);
			GCHandle surfaceIntersectionHandle	= GCHandle.Alloc(surfaceIntersection, GCHandleType.Pinned);
			GCHandle worldIntersectionHandle	= GCHandle.Alloc(worldIntersection, GCHandleType.Pinned);
			GCHandle surfaceInvertedHandle		= GCHandle.Alloc(surfaceInverted, GCHandleType.Pinned);
			GCHandle planesHandle				= GCHandle.Alloc(planes, GCHandleType.Pinned);

			IntPtr distancePtr				= distanceHandle.AddrOfPinnedObject();
			IntPtr uniqueIDPtr				= uniqueIDHandle.AddrOfPinnedObject();
			IntPtr brushIDPtr				= brushIDHandle.AddrOfPinnedObject();
			IntPtr surfaceIndexPtr			= surfaceIndexHandle.AddrOfPinnedObject();
			IntPtr texGenIndexPtr			= texGenIndexHandle.AddrOfPinnedObject();
			IntPtr surfaceIntersectionPtr	= surfaceIntersectionHandle.AddrOfPinnedObject();
			IntPtr worldIntersectionPtr		= worldIntersectionHandle.AddrOfPinnedObject();
			IntPtr surfaceInvertedPtr		= surfaceInvertedHandle.AddrOfPinnedObject();
			IntPtr planesPtr				= planesHandle.AddrOfPinnedObject();

			var result = RayCastIntoModelMultiGet(intersectionCount,
												  distancePtr,
												  uniqueIDPtr,
												  brushIDPtr,
												  surfaceIndexPtr,
												  texGenIndexPtr,
												  surfaceIntersectionPtr,
												  worldIntersectionPtr,
												  surfaceInvertedPtr,
												  planesPtr);

			distanceHandle.Free();
			uniqueIDHandle.Free();
			brushIDHandle.Free();
			surfaceIndexHandle.Free();
			texGenIndexHandle.Free();
			surfaceIntersectionHandle.Free();
			worldIntersectionHandle.Free();
			surfaceInvertedHandle.Free();
			planesHandle.Free();

			if (!result)
				return null;
			
			var intersections = new CoreIntersection[intersectionCount];
			
			for (int i = 0, t = 0; i < intersectionCount; i++, t+=3)
			{
				intersections[i] = new CoreIntersection
				{
					distance			= distance[i],
					modelID				= modelID,
					brushID				= brushID[i],
					surfaceIndex		= surfaceIndex[i],
					texGenIndex			= texGenIndex[i],
					surfaceIntersection = surfaceIntersection[i],
					worldIntersection	= worldIntersection[i],
					surfaceInverted		= (surfaceInverted[i] == 1),
					plane				= planes[i]
				};
			}
			return intersections;
        }
		#endregion

		#endregion
	}



	public struct CoreOperation
	{
		internal Int32 operationID;
		internal Int32 nodeID;

		public static CoreOperation Create(Int32 uniqueID, string debugName) // remove necessity to have a debugname
		{
			var operation = new CoreOperation();
			if (!GenerateOperationID(uniqueID, debugName, out operation.operationID, out operation.nodeID))
			{
				operation.operationID = 0;
				operation.nodeID = 0;
				return operation;
			}
			return operation;
		}

		public Int32[]			ChildNodeIDs	{ get { return GetOperationChildren(operationID); } set { SetOperationChildren(operationID, value); } }

		public bool				IsValid			{ get { return (operationID != 0) && (nodeID != 0); } }
		public Int32			NodeID			{ get { return nodeID; } }
		public CSGOperationType Operation		{ get { return (CSGOperationType)GetOperationOperationType(operationID); } set { SetOperationOperationType(operationID, value); } }
		public Int32			UniqueID		{ get { return GetOperationUniqueID(operationID); } }

		// TODO: make this get only, return CoreOperation / CoreModel
		public Int32			ParentOperation	{ get { return GetOperationParent(operationID); } set	{ SetOperationParent(operationID, value); } }
		public Int32			Model			{ get { return GetOperationModel(operationID); } set	{ SetOperationModel(operationID, value); } }

		public void SetDirty() { CoreAPI.UpdateNode(nodeID); }

		public void Destroy() { RemoveOperation(operationID); operationID = 0; nodeID = 0; }

		#region Operation C++ functions
		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GenerateOperationID(Int32		uniqueID,
													   [In] string	name,
													   out Int32	generatedOperationID,
													   out Int32	generatedNodeID);

		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool RemoveOperation(Int32 operationID);

		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern Int32 GetOperationUniqueID(Int32 operationID);


		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetOperationHierarchy(Int32		operationID,
														 Int32		modelID,
														 Int32		parentID);
		
		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern Int32 GetOperationParent(Int32		operationID);
		
		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetOperationParent(Int32		operationID,
													  Int32		parentOperationID);
		
		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern Int32 GetOperationModel(Int32		operationID);

		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetOperationModel(Int32		operationID,
													 Int32		modelID);


		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern Int32 GetOperationOperationType(Int32 operationID);

		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool SetOperationOperationType(Int32 operationID,
															 CSGOperationType operation);

		

		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool SetOperationChildren(Int32	operationID,
														Int32	childCount,
														IntPtr	childrenNodeIDs);

		private static bool SetOperationChildren(Int32		operationID, 
												 Int32[]	childNodeIDs)
		{
			GCHandle	childNodeIDsHandle	= GCHandle.Alloc(childNodeIDs, GCHandleType.Pinned);
			IntPtr		childNodeIDsPtr		= childNodeIDsHandle.AddrOfPinnedObject();

			var result = SetOperationChildren(operationID,
											  childNodeIDs.Length,
											  childNodeIDsPtr);

			childNodeIDsHandle.Free();
			return result;
		}


		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern Int32 GetOperationChildCount(Int32 operationID);

		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool GetOperationChildren(Int32	operationID,
														Int32	childCount,
														IntPtr	childNodeIDs);
		private static Int32[] GetOperationChildren(Int32 operationID)
		{
			var childCount = GetOperationChildCount(operationID);
			var childNodeIDs = new Int32[childCount];
			if (childCount == 0)
				return childNodeIDs;

			GCHandle childNodeIDsHandle = GCHandle.Alloc(childNodeIDs, GCHandleType.Pinned);
			IntPtr childNodeIDsPtr = childNodeIDsHandle.AddrOfPinnedObject();
			GetOperationChildren(operationID,
								 childCount,
								 childNodeIDsPtr);
			childNodeIDsHandle.Free();
			return childNodeIDs;
		}
		#endregion
	}



	// TODO: - do not store transformation in -shape-
	//		 - combine shape and control-mesh (except the parts that are dependent on transformation, keep those in brush)
	//		 - register control-mesh somewhere in some sort of manager, keep track of unique control-meshes using hashes
	//		 - assign control-mesh to brush using index into list of control-meshes

	// TODO: - add ability to generate outlines for brush

	public struct CoreBrush
	{
		internal Int32 brushID;
		internal Int32 nodeID;

		public static CoreBrush Create(Int32 uniqueID, string debugName) // remove necessity to have a debugname
		{
			var brush = new CoreBrush();
			if (!GenerateBrushID(uniqueID, debugName, out brush.brushID, out brush.nodeID))
			{
				brush.brushID = 0;
				brush.nodeID = 0;
				return brush;
			}
			return brush;
		}
		
		public bool				IsValid			{ get { return (brushID != 0) && (nodeID != 0); } }
		public Int32			NodeID			{ get { return nodeID; } }
		public CSGOperationType Operation		{ get { return (CSGOperationType)GetBrushOperationType(brushID); } set { SetBrushOperationType(brushID, value); } }
		
		public bool				Infinite		{ get { return GetBrushInfinity(brushID); }	set	{ SetBrushInfinity(brushID, value); } }
		public Int32			UniqueID		{ get { return GetBrushUniqueID(brushID); } }

		public Bounds			Bounds			{ get { return GetBrushBounds(brushID); } }

		public Matrix4x4		LocalToWorld	{ get { return GetBrushLocalToWorld(brushID); } set { SetBrushLocalToWorld(brushID, ref value, Vector3.zero); } }
		
		// TODO: make this get only, return CoreOperation / CoreModel
		public Int32			ParentOperation	{ get { return GetBrushParent(brushID); }	set	{ SetBrushParent(brushID, value); } }
		public Int32			Model			{ get { return GetBrushModel(brushID); }	set { SetBrushModel(brushID, value); } }

		public void SetDirty() { CoreAPI.UpdateNode(nodeID); }

		public void Destroy() { RemoveBrush(brushID); brushID = 0; nodeID = 0; }
		
		public CoreIntersection RayCast(Vector3 rayStart, Vector3 rayEnd, bool ignoreInvisiblePolygons = true)
		{
			return RayCastIntoBrush(brushID, rayStart, rayEnd, ignoreInvisiblePolygons);
		}


		#region Brush C++ functions
		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)] private static extern bool GenerateBrushID(Int32 uniqueID, [In] string name, out Int32 generatedBrushID, out Int32 generatedNodeID);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool RemoveBrush(Int32 brushID);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern Int32 GetBrushUniqueID(Int32 brushID);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool GetBrushInfinity(Int32 brushID);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool SetBrushInfinity(Int32 brushID, bool isInfinite);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern Int32 GetBrushOperationType(Int32 brushID);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool SetBrushOperationType(Int32 brushID, CSGOperationType operation);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern Int32 GetBrushParentNode(Int32 brushID);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool SetBrushParentNode(Int32 brushID, Int32 parentID);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern Int32 GetBrushParent(Int32 brushID);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool SetBrushParent(Int32 brushID, Int32 parentID);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern Int32 GetBrushModel(Int32 brushID);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool SetBrushModel(Int32 brushID, Int32 modelID);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool GetBrushLocalToWorld(Int32 brushID, [Out] out Matrix4x4 localToWorld);

		private static Matrix4x4 GetBrushLocalToWorld(Int32 brushID)
		{
			Matrix4x4 result = Matrix4x4.identity;
			if (GetBrushLocalToWorld(brushID, out result))
				return result;
			return Matrix4x4.identity;
		}

		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool SetBrushLocalToWorld(Int32 brushID, [In] ref Matrix4x4 localToWorld, Vector3 t);
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern bool GetBrushBounds(Int32 brushID, ref AABB bounds);

		internal static Bounds GetBrushBounds(Int32 brushID)
		{
			AABB aabb = new AABB();
			if (GetBrushBounds(brushID, ref aabb))
			{
				Bounds bounds = new Bounds(aabb.Center, aabb.Size);
				return bounds;
			}
			return new Bounds();
		}


		#region RayCastIntoBrush
		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RayCastIntoBrush(Int32			brushID,
													Int32			texGenID,
													[In]ref Vector3	rayStart,
													[In]ref Vector3	rayEnd,
                                                    out Int32		surfaceIndex,
													out Vector2		surfaceIntersection,
                                                    out Vector3		worldIntersection,
													out bool		surfaceInverted,
                                                    out CSGPlane	plane);

		private static CoreIntersection RayCastIntoBrush(Int32		brushID, 
														 Vector3	rayStart,
														 Vector3	rayEnd)
        {
			if (brushID == -1)
			{
				return null;
			}
			
			Int32		surfaceIndex;
			Vector2		surfaceIntersection;
			Vector3		worldIntersection;
			bool		surfaceInverted;
			CSGPlane	plane;
			if (!RayCastIntoBrush(brushID, ref rayStart, ref rayEnd, 
								  out surfaceIndex, out texGenIndex, out surfaceIntersection, 
								  out worldIntersection, out surfaceInverted, out plane))
			{
				return null;
			}

			return new CoreIntersection
			{
				brushID				= brushID,
				surfaceIndex		= surfaceIndex,
				texGenIndex			= texGenIndex,
				surfaceIntersection = surfaceIntersection,
				worldIntersection	= worldIntersection,
				surfaceInverted		= surfaceInverted,
				plane				= plane,
				modelID				= -1
			};
		}
		#endregion

		#endregion
	}



	public static class CoreAPI
	{
#if DEMO
		internal const string NativePluginName = "RealtimeCSG-DEMO-Native";
#else
		internal const string NativePluginName = "RealtimeCSG[" + ToolConstants.NativeVersion + "]";
#endif

		#region Node C++ functions
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern bool UpdateNode(Int32 nodeID);
		#endregion

		#region Models C++ functions
		[DllImport(CoreAPI.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool	RemoveModels	(Int32		modelCount,
													 IntPtr		modelIDs);
		internal static bool		RemoveModels	(Int32		modelCount,
													 Int32[]	modelIDs)
		{
			GCHandle modelIDsHandle = GCHandle.Alloc(modelIDs, GCHandleType.Pinned);
			IntPtr modelIDsPtr = modelIDsHandle.AddrOfPinnedObject();
			var result = RemoveModels(modelCount,
									  modelIDsPtr);
			modelIDsHandle.Free();
			return result;
		}
		#endregion
		
		#region Operation C++ functions
		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RemoveOperations	(Int32			operationCount,
													 IntPtr			operationIDs);
		private static bool RemoveOperations(Int32		operationCount,
											 Int32[]	operationIDs)
		{
			GCHandle	operationIDsHandle	= GCHandle.Alloc(operationIDs, GCHandleType.Pinned);
			IntPtr		operationIDsPtr		= operationIDsHandle.AddrOfPinnedObject();

			var result = RemoveOperations(operationCount,
										  operationIDsPtr);

			operationIDsHandle.Free();
			return result;
		}
		#endregion

		#region Brush C++ functions
		[DllImport(CoreAPI.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RemoveBrushes(Int32			brushCount,
												 IntPtr			brushIDs);
		private static bool RemoveBrushes(Int32 brushCount,
										  Int32[] brushIDs)
		{
			GCHandle	brushIDsHandle	= GCHandle.Alloc(brushIDs, GCHandleType.Pinned);
			IntPtr		brushIDsPtr		= brushIDsHandle.AddrOfPinnedObject();

			var result = RemoveBrushes(brushCount,
									   brushIDsPtr);

			brushIDsHandle.Free();
			return result;
		}
		#endregion

	}
}
#endif