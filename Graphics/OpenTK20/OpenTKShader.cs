using DeltaEngine.Content;
using OpenTK.Graphics.OpenGL;
using System;
using DeltaEngine.Core;
using DeltaEngine.Datatypes;
using DeltaEngine.Extensions;

namespace DeltaEngine.Graphics.OpenTK20
{
	public class OpenTKShader : ShaderWithFormat
	{
		private readonly OpenTK20Device device;
		protected int programHandle;
		protected const int InvalidShaderHandle = -1;
		protected int vertexShaderHandle;
		protected int fragmentShaderHandle;
		private const string AttributePrefix = "a";
		private int[] attributeLocations;
		private int diffuseTextureUniformLocation;
		private int lightmapTextureUniformLocation;
		private int modelViewProjectionMatrixLocation;
		private int jointMatricesLocation;
		private int modelViewMatrixLocation;
		private int viewMatrixLocation;
		private int normalMatrixLocation;
		private int fogColorUniformLocation;
		private int fogStartUniformLocation;
		private int fogEndUniformLocation;
		private int lightDirectionUniformLocation;
		private int lightColorUniformLocation;
		private Matrix viewMatrix;
		private Matrix modelViewMatrix;
		private Matrix modelViewProjectionMatrix;

		public OpenTKShader(ShaderWithFormatCreationData creationData, OpenTK20Device device)
			: this((ShaderCreationData)creationData, device) {}

		public OpenTKShader(ShaderCreationData data, OpenTK20Device device)
			: base(data)
		{
			this.device = device;
			TryCreateShader();
		}

		protected void CreateNewShaderProgramHandle()
		{
			programHandle = GL.CreateProgram();
		}

		protected void CompileVertexShader()
		{
			vertexShaderHandle = CreateCompiledSubShader(ShaderType.VertexShader, OpenGLVertexCode);
		}

		private static int CreateCompiledSubShader(ShaderType shaderType, string subShaderCode)
		{
			int shaderHandle = GL.CreateShader(shaderType);
			int codeLength = subShaderCode.Length;
			GL.ShaderSource(shaderHandle, 1, new string[] { subShaderCode }, ref codeLength);
			GL.CompileShader(shaderHandle);
			return shaderHandle;
		}

		protected string GetShaderCompileInfo(int shaderHandle)
		{
			return GL.GetShaderInfoLog(shaderHandle);
		}

		protected bool IsShaderCompiled(int shaderHandle)
		{
			int compileState;
			GL.GetShader(shaderHandle, ShaderParameter.CompileStatus, out compileState);
			return compileState != 0;
		}

		protected void DeleteShaderHandle(int shaderHandle)
		{
			GL.DeleteShader(shaderHandle);
		}

		protected void CompileFragmentShader()
		{
			fragmentShaderHandle = CreateCompiledSubShader(ShaderType.FragmentShader, OpenGLFragmentCode);
		}

		protected void LinkVertexAndFragmentShaderToProgram()
		{
			GL.AttachShader(programHandle, vertexShaderHandle);
			GL.AttachShader(programHandle, fragmentShaderHandle);
			GL.LinkProgram(programHandle);
		}

		protected sealed override void CreateShader()
		{
			try
			{
				CreateShaderProgramAndLoadShaderParameters();
			}
			catch (UnableToCreateNewShaderHandle)
			{
				new ShaderCreationHasFailed("Can not create shader program");
			}
		}

		private void CreateShaderProgramAndLoadShaderParameters()
		{
			CreateShaderProgram();
			LoadAttributeLocations();
			LoadUniformLocations();
		}

		private void CreateShaderProgram()
		{
			CreateNewShaderProgramHandle();
			if (programHandle == InvalidShaderHandle)
				throw new UnableToCreateNewShaderHandle();
			CreateVertexShader();
			CreateFragmentShader();
			LinkVertexAndFragmentShaderToProgram();
		}

		private void CreateVertexShader()
		{
			CompileVertexShader();
			CheckIfShaderCompiledSuccessfully(ref vertexShaderHandle);
		}

		private void CheckIfShaderCompiledSuccessfully(ref int shaderHandle)
		{
			if (StackTraceExtensions.StartedFromNCrunchOrNunitConsole)
				return;
			string fragmentShaderCompileInfo = GetShaderCompileInfo(shaderHandle);
			if (IsShaderCompiled(shaderHandle))
				return;
			DeleteShaderHandle(shaderHandle);
			string shaderType = shaderHandle == fragmentShaderHandle ? "Fragment Shader" : "Vertex Shader";
			shaderHandle = InvalidShaderHandle;
			throw new UnableToCompileShader(shaderType, fragmentShaderCompileInfo);
		}

		private void CreateFragmentShader()
		{
			CompileFragmentShader();
			CheckIfShaderCompiledSuccessfully(ref fragmentShaderHandle);
		}

		private void LoadAttributeLocations()
		{
			attributeLocations = new int[Format.Elements.Length];
			for (int i = 0; i < Format.Elements.Length; i++)
			{
				string identifier = AttributePrefix + Format.Elements[i].ElementType.ToString().Replace("2D", "").Replace("3D", "");
				attributeLocations[i] = device.GetShaderAttributeLocation(programHandle, identifier);
				if (attributeLocations[i] < 0)
					Logger.Warning("Attribute " + identifier + " not found in " + this);
			}
		}

		private void LoadUniformLocations()
		{
			diffuseTextureUniformLocation = device.GetShaderUniformLocation(programHandle, "Texture");
			lightmapTextureUniformLocation = device.GetShaderUniformLocation(programHandle, "Lightmap");
			modelViewProjectionMatrixLocation = device.GetShaderUniformLocation(programHandle, "ModelViewProjection");
			modelViewMatrixLocation = device.GetShaderUniformLocation(programHandle, "ModelView");
			viewMatrixLocation = device.GetShaderUniformLocation(programHandle, "View");
			normalMatrixLocation = device.GetShaderUniformLocation(programHandle, "Normal");
			jointMatricesLocation = device.GetShaderUniformLocation(programHandle, "JointTransforms");
			fogColorUniformLocation = device.GetShaderUniformLocation(programHandle, "fogColor");
			fogStartUniformLocation = device.GetShaderUniformLocation(programHandle, "fogStart");
			fogEndUniformLocation = device.GetShaderUniformLocation(programHandle, "fogEnd");
			lightDirectionUniformLocation = device.GetShaderUniformLocation(programHandle, "lightDir");
			lightColorUniformLocation = device.GetShaderUniformLocation(programHandle, "lightColor");
		}

		public override void SetModelViewProjection(Matrix matrix)
		{
			device.SetUniformValue(modelViewProjectionMatrixLocation, matrix);
		}

		public override void SetModelViewProjection(Matrix model, Matrix view, Matrix projection)
		{
			viewMatrix = view;
			modelViewMatrix = model * view;
			modelViewProjectionMatrix = modelViewMatrix * projection;
			device.SetUniformValue(modelViewProjectionMatrixLocation, modelViewProjectionMatrix);
		}

		public override void SetJointMatrices(Matrix[] jointMatrices)
		{
			device.SetUniformValues(jointMatricesLocation, jointMatrices);
		}

		public override void SetDiffuseTexture(Image texture)
		{
			device.BindTexture((texture as OpenGL20Image).Handle);
			device.SetUniformValue(diffuseTextureUniformLocation, 0);
		}

		public override void SetLightmapTexture(Image texture)
		{
			device.BindTexture((texture as OpenGL20Image).Handle, 1);
			device.SetUniformValue(lightmapTextureUniformLocation, 1);
		}

		public override void SetSunLight(SunLight light)
		{
			device.SetUniformValue(viewMatrixLocation, viewMatrix);
			device.SetUniformValue(normalMatrixLocation, Matrix.InverseTranspose(modelViewMatrix));
			SetUniformColorValue(lightColorUniformLocation, light.Color);
			device.SetUniformValue(lightDirectionUniformLocation, light.Direction);
		}

		private void SetUniformColorValue(int uniformLocation, Color value)
		{
			device.SetUniformValue(uniformLocation, value.RedValue, value.GreenValue, value.BlueValue, value.AlphaValue);
		}

		public override void Bind()
		{
			device.Shader = this;
			device.UseShaderProgram(programHandle);
		}

		public override void BindVertexDeclaration()
		{
			for (int i = 0; i < Format.Elements.Length; i++)
				if (Format.Elements[i].Size == Format.Elements[i].ComponentCount)
					device.DefineVertexAttributeWithBytes(attributeLocations[i], Format.Elements[i].ComponentCount, Format.Stride, Format.Elements[i].Offset);
				else
					device.DefineVertexAttributeWithFloats(attributeLocations[i], Format.Elements[i].ComponentCount, Format.Stride, Format.Elements[i].Offset);
		}

		public override void ApplyFogSettings(FogSettings fogSettings)
		{
			device.SetUniformValue(modelViewMatrixLocation, modelViewMatrix);
			SetUniformColorValue(fogColorUniformLocation, fogSettings.FogColor);
			device.SetUniformValue(fogStartUniformLocation, fogSettings.FogStart);
			device.SetUniformValue(fogEndUniformLocation, fogSettings.FogEnd);
		}

		protected override void DisposeData()
		{
			device.DeleteShaderProgram(programHandle);
		}

		public class UnableToCreateNewShaderHandle : Exception {}

		public class UnableToCompileShader : Exception
		{
			public UnableToCompileShader(string shaderType, string reason)
				: base("Compilation of " + shaderType + " has failed because: " + reason) {}
		}
	}
}