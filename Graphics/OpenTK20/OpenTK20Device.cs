using System;
using DeltaEngine.Core;
using DeltaEngine.Datatypes;
using DeltaEngine.Extensions;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using DeltaEngine.Graphics.Vertices;

namespace DeltaEngine.Graphics.OpenTK20
{
	public sealed class OpenTK20Device : Device
	{
		private readonly IWindowInfo windowInfo;
		private const int BitsPerPixel = 32;
		private BlendMode currentBlendMode = BlendMode.Opaque;
		public const int InvalidHandle = -1;
		private bool isCullingEnabled;

		public GraphicsContext Context { get; private set; }

		public OpenTK20Device(Window window)
			: base(window)
		{
			windowInfo = Utilities.CreateWindowsWindowInfo(window.Handle);
			CreateContext();
			SetViewport(window.ViewportPixelSize);
			SetupFrontFaceDirection();
		}

		private void CreateContext()
		{
			ColorFormat colorFormat = new ColorFormat(8, 8, 8, 8);
			GraphicsMode graphicsMode = new GraphicsMode(colorFormat, 24, 8, 0);
			Context = new GraphicsContext(graphicsMode, windowInfo);
			if (Settings.Current.UseVSync)
				Context.SwapInterval = Settings.Current.LimitFramerate;
			Context.MakeCurrent(windowInfo);
			Context.LoadAll();
			string version = GL.GetString(StringName.Version);
			int majorVersion = int.Parse(version[0] + "");
			if (majorVersion < 3 || string.IsNullOrEmpty(GL.GetString(StringName.Extensions)))
				throw new OpenGLVersionDoesNotSupportShaders();
		}

		public override void SetViewport(Size viewportSize)
		{
			if (Settings.Current.StartInFullscreen)
			{
				DisplayResolution resolution = DisplayDevice.Default.SelectResolution((int)viewportSize.Width, (int)viewportSize.Height, BitsPerPixel, 0);
				DisplayDevice.Default.ChangeResolution(resolution);
				if (resolution.Width != (int)viewportSize.Width || resolution.Height != (int)viewportSize.Height)
					throw new ResolutionRequestFailedCouldNotFindResolution(viewportSize);
			}
			GL.Viewport(0, 0, (int)viewportSize.Width, (int)viewportSize.Height);
			SetModelViewProjectionMatrixFor2D();
		}

		private static void SetupFrontFaceDirection()
		{
			GL.FrontFace(FrontFaceDirection.Ccw);
		}

		public override void Clear()
		{
			Color color = window.BackgroundColor;
			if (color.A == 0)
				return;
			GL.ClearColor(color.RedValue, color.GreenValue, color.BlueValue, color.AlphaValue);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}

		public override void Present()
		{
			if (!StackTraceExtensions.StartedFromNCrunchOrNunitConsole && Context.IsCurrent && !Context.IsDisposed)
				Context.SwapBuffers();
		}

		public override void Dispose()
		{
			Context.Dispose();
		}

		public override void SetBlendMode(BlendMode blendMode)
		{
			if (currentBlendMode == blendMode)
				return;
			currentBlendMode = blendMode;
			switch (blendMode)
			{
				case BlendMode.Opaque:
					GL.Disable(EnableCap.Blend);
					break;
				case BlendMode.Normal:
					GL.Enable(EnableCap.Blend);
					GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
					GL.BlendEquation(BlendEquationMode.FuncAdd);
					break;
				case BlendMode.AlphaTest:
					GL.Disable(EnableCap.Blend);
					GL.Enable(EnableCap.AlphaTest);
					break;
				case BlendMode.Additive:
					GL.Enable(EnableCap.Blend);
					GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
					GL.BlendEquation(BlendEquationMode.FuncAdd);
					break;
				case BlendMode.Subtractive:
					GL.Enable(EnableCap.Blend);
					GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
					GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);
					break;
				case BlendMode.LightEffect:
					GL.Enable(EnableCap.Blend);
					GL.BlendFunc(BlendingFactorSrc.DstColor, BlendingFactorDest.One);
					GL.BlendEquation(BlendEquationMode.FuncAdd);
					break;
			}
		}

		public int CreateVertexBuffer(int sizeInBytes, OpenGL20BufferMode mode)
		{
			int bufferHandle;
			GL.GenBuffers(1, out bufferHandle);
			GL.BindBuffer(BufferTarget.ArrayBuffer, bufferHandle);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)sizeInBytes, IntPtr.Zero, GetBufferMode(mode));
			return bufferHandle;
		}

		private static BufferUsageHint GetBufferMode(OpenGL20BufferMode mode)
		{
			return mode == OpenGL20BufferMode.Static ? BufferUsageHint.StaticDraw : mode == OpenGL20BufferMode.Dynamic ? BufferUsageHint.DynamicDraw : BufferUsageHint.StreamDraw;
		}

		public void BindVertexBuffer(int bufferHandle)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, bufferHandle);
		}

		public void LoadVertexData<T>(int offset, T[] vertices, int vertexDataSizeInBytes)
			where T : struct
		{
			GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)offset, (IntPtr)vertexDataSizeInBytes, vertices);
		}

		public int CreateIndexBuffer(int sizeInBytes, OpenGL20BufferMode mode)
		{
			int bufferHandle;
			GL.GenBuffers(1, out bufferHandle);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, bufferHandle);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)sizeInBytes, IntPtr.Zero, GetBufferMode(mode));
			return bufferHandle;
		}

		public void BindIndexBuffer(int bufferHandle)
		{
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, bufferHandle);
		}

		public void LoadIndices(int offset, short[] indices, int indexDataSizeInBytes)
		{
			GL.BufferSubData(BufferTarget.ElementArrayBuffer, (IntPtr)offset, (IntPtr)(indexDataSizeInBytes), indices);
		}

		public void DeleteBuffer(int bufferHandle)
		{
			GL.DeleteBuffers(1, ref bufferHandle);
		}

		private void NativeEnableCulling()
		{
			GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Back);
		}

		private void NativeDisableCulling()
		{
			GL.Disable(EnableCap.CullFace);
		}

		public override void EnableDepthTest()
		{
			GL.Enable(EnableCap.DepthTest);
		}

		public override void DisableDepthTest()
		{
			GL.Disable(EnableCap.DepthTest);
		}

		public int GenerateTexture()
		{
			int glHandle;
			GL.GenTextures(1, out glHandle);
			return glHandle;
		}

		public void BindTexture(int glHandle, int samplerIndex = 0)
		{
			GL.ActiveTexture(GetSamplerFrom(samplerIndex));
			GL.BindTexture(TextureTarget.Texture2D, glHandle);
		}

		private static TextureUnit GetSamplerFrom(int samplerIndex)
		{
			if (samplerIndex == 0)
				return TextureUnit.Texture0;
			if (samplerIndex == 1)
				return TextureUnit.Texture1;
			throw new UnsupportedTextureUnit();
		}

		public void LoadTextureInNativePlatformFormat(int width, int height, IntPtr nativeLoadedData, bool hasAlpha)
		{
			GL.TexImage2D(TextureTarget.Texture2D, 0, hasAlpha ? PixelInternalFormat.Rgba : PixelInternalFormat.Rgb, width, height, 0, hasAlpha ? PixelFormat.Bgra : PixelFormat.Bgr, PixelType.UnsignedByte, nativeLoadedData);
		}

		public void FillTexture(Size size, byte[] rgbaData, bool hasAlpha)
		{
			GL.TexImage2D(TextureTarget.Texture2D, 0, hasAlpha ? PixelInternalFormat.Rgba : PixelInternalFormat.Rgb, (int)size.Width, (int)size.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, rgbaData);
		}

		public void DeleteTexture(int glHandle)
		{
			GL.DeleteTextures(1, ref glHandle);
		}

		public void SetTextureSamplerState(bool disableLinearFiltering = false, bool allowTiling = false)
		{
			TextureMinFilter minFilter = disableLinearFiltering ? TextureMinFilter.Nearest : TextureMinFilter.Linear;
			TextureMagFilter magFilter = disableLinearFiltering ? TextureMagFilter.Nearest : TextureMagFilter.Linear;
			TextureWrapMode clampMode = allowTiling ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;
			SetSamplerState((int)minFilter, (int)magFilter, (int)clampMode);
		}

		private static void SetSamplerState(int textureMinFilter, int textureMagFilter, int clampMode)
		{
			SetTexture2DParameter(TextureParameterName.TextureMinFilter, textureMinFilter);
			SetTexture2DParameter(TextureParameterName.TextureMagFilter, textureMagFilter);
			SetTexture2DParameter(TextureParameterName.TextureWrapS, clampMode);
			SetTexture2DParameter(TextureParameterName.TextureWrapT, clampMode);
		}

		private static void SetTexture2DParameter(TextureParameterName name, int value)
		{
			GL.TexParameter(TextureTarget.Texture2D, name, value);
		}

		public void UseShaderProgram(int programHandle)
		{
			GL.UseProgram(programHandle);
		}

		public void DeleteShaderProgram(int programHandle)
		{
			GL.DeleteProgram(programHandle);
		}

		public int GetShaderAttributeLocation(int programHandle, string attributeName)
		{
			return GL.GetAttribLocation(programHandle, attributeName);
		}

		public int GetShaderUniformLocation(int programHandle, string uniformName)
		{
			return GL.GetUniformLocation(programHandle, uniformName);
		}

		public void DefineVertexAttributeWithFloats(int attributeLocation, int numberOfFloatComponents, int vertexTotalSize, int attributeOffsetInVertex)
		{
			GL.EnableVertexAttribArray(attributeLocation);
			GL.VertexAttribPointer(attributeLocation, numberOfFloatComponents, VertexAttribPointerType.Float, false, vertexTotalSize, (IntPtr)attributeOffsetInVertex);
		}

		public void DefineVertexAttributeWithBytes(int attributeLocation, int numberOfByteComponents, int vertexTotalSize, int attributeOffsetInVertex)
		{
			GL.EnableVertexAttribArray(attributeLocation);
			GL.VertexAttribPointer(attributeLocation, numberOfByteComponents, VertexAttribPointerType.UnsignedByte, true, vertexTotalSize, (IntPtr)attributeOffsetInVertex);
		}

		public void SetUniformValue(int location, int value)
		{
			GL.Uniform1(location, value);
		}

		public void SetUniformValue(int location, float value)
		{
			GL.Uniform1(location, value);
		}

		public void SetUniformValue(int location, Matrix matrix)
		{
			GL.UniformMatrix4(location, 1, false, matrix.GetValues);
		}

		public void SetUniformValue(int location, Vector3D vector)
		{
			GL.Uniform3(location, vector.X, vector.Y, vector.Z);
		}

		public void SetUniformValue(int location, float r, float g, float b, float a)
		{
			GL.Uniform4(location, r, g, b, a);
		}

		public void SetUniformValues(int uniformLocation, Matrix[] matrices)
		{
			float[] values = new float[matrices.Length * 16];
			for (int matrixIndex = 0; matrixIndex < matrices.Length; ++matrixIndex)
				matrices[matrixIndex].GetValues.CopyTo(values, matrixIndex * 16);
			GL.UniformMatrix4(uniformLocation, matrices.Length, false, values);
		}

		public void DrawTriangles(int indexOffsetInBytes, int numberOfIndicesToRender)
		{
			Shader.BindVertexDeclaration();
			GL.DrawElements(PrimitiveType.Triangles, numberOfIndicesToRender, DrawElementsType.UnsignedShort, indexOffsetInBytes);
		}

		public void DrawLines(int vertexOffset, int verticesCount)
		{
			Shader.BindVertexDeclaration();
			GL.DrawArrays(PrimitiveType.Lines, vertexOffset, verticesCount);
		}

		public void ReadPixels(Rectangle frame, byte[] bufferToStoreData)
		{
			GL.ReadPixels((int)frame.Left, (int)frame.Top, (int)frame.Width, (int)frame.Height, PixelFormat.Rgb, PixelType.UnsignedByte, bufferToStoreData);
		}

		public override CircularBuffer CreateCircularBuffer(ShaderWithFormat shader, BlendMode blendMode, VerticesMode drawMode = VerticesMode.Triangles)
		{
			return new OpenGL20CircularBuffer(this, shader, blendMode, drawMode);
		}

		protected override void EnableClockwiseBackfaceCulling()
		{
			if (isCullingEnabled)
				return;
			isCullingEnabled = true;
			NativeEnableCulling();
		}

		protected override void DisableCulling()
		{
			if (!isCullingEnabled)
				return;
			isCullingEnabled = false;
			NativeDisableCulling();
		}

		private class OpenGLVersionDoesNotSupportShaders : Exception {}

		private class ResolutionRequestFailedCouldNotFindResolution : Exception
		{
			public ResolutionRequestFailedCouldNotFindResolution(Size resolution)
				: base(resolution.ToString()) {}
		}

		private class UnsupportedTextureUnit : Exception {}
	}
}