using System;

namespace DeltaEngine.Graphics.OpenTK20
{
	public class OpenGL20Geometry : Geometry
	{
		private readonly OpenTK20Device device;
		private int vertexBufferHandle = OpenTK20Device.InvalidHandle;
		private int indexBufferHandle = OpenTK20Device.InvalidHandle;

		protected OpenGL20Geometry(string contentName, OpenTK20Device device)
			: base(contentName)
		{
			this.device = device;
		}

		private OpenGL20Geometry(GeometryCreationData creationData, OpenTK20Device device)
			: base(creationData)
		{
			this.device = device;
		}

		protected override void SetNativeData(byte[] vertexData, short[] indices)
		{
			if (vertexBufferHandle == OpenTK20Device.InvalidHandle)
				CreateBuffers();
			device.LoadVertexData(0, vertexData, vertexData.Length);
			device.LoadIndices(0, indices, indices.Length * sizeof(short));
		}

		private void CreateBuffers()
		{
			int vertexDataSize = NumberOfVertices * Format.Stride;
			vertexBufferHandle = device.CreateVertexBuffer(vertexDataSize, OpenGL20BufferMode.Static);
			if (vertexBufferHandle == OpenTK20Device.InvalidHandle)
				throw new UnableToCreateOpenGLGeometry();
			indexBufferHandle = device.CreateIndexBuffer(NumberOfIndices * sizeof(short), OpenGL20BufferMode.Static);
		}

		public override void Draw()
		{
			if (vertexBufferHandle == OpenTK20Device.InvalidHandle)
				throw new UnableToDrawDynamicGeometrySetDataNeedsToBeCalledFirst();
			device.BindVertexBuffer(vertexBufferHandle);
			device.BindIndexBuffer(indexBufferHandle);
			device.DrawTriangles(0, NumberOfIndices);
		}

		protected override void DisposeData()
		{
			if (vertexBufferHandle == OpenTK20Device.InvalidHandle)
				return;
			device.DeleteBuffer(vertexBufferHandle);
			device.DeleteBuffer(indexBufferHandle);
			vertexBufferHandle = OpenTK20Device.InvalidHandle;
		}

		private class UnableToCreateOpenGLGeometry : Exception {}

		private class UnableToDrawDynamicGeometrySetDataNeedsToBeCalledFirst : Exception {}
	}
}