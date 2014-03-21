using System;
using DeltaEngine.Datatypes;
using DeltaEngine.Multimedia.OpenTK.Helpers;
using OpenTK;
using OpenTK.Audio.OpenAL;

namespace DeltaEngine.Multimedia.OpenTK
{
	public sealed class OpenTKSoundDevice : SoundDevice
	{
		private IntPtr deviceHandle;
		private readonly global::OpenTK.ContextHandle context;

		public OpenTKSoundDevice()
		{
			deviceHandle = global::OpenTK.Audio.OpenAL.Alc.OpenDevice("");
			context = global::OpenTK.Audio.OpenAL.Alc.CreateContext(deviceHandle, new int[0]);
			global::OpenTK.Audio.OpenAL.Alc.MakeContextCurrent(context);
		}

		public override void Dispose()
		{
			base.Dispose();
			if (deviceHandle == IntPtr.Zero)
				return;
			global::OpenTK.Audio.OpenAL.Alc.DestroyContext(context);
			global::OpenTK.Audio.OpenAL.Alc.CloseDevice(deviceHandle);
			deviceHandle = IntPtr.Zero;
		}

		public int CreateBuffer()
		{
			return global::OpenTK.Audio.OpenAL.AL.GenBuffer();
		}

		public int[] CreateBuffers(int numberOfBuffers)
		{
			return global::OpenTK.Audio.OpenAL.AL.GenBuffers(numberOfBuffers);
		}

		public void DeleteBuffer(int bufferHandle)
		{
			global::OpenTK.Audio.OpenAL.AL.DeleteBuffer(bufferHandle);
		}

		public void DeleteBuffers(int[] bufferHandles)
		{
			global::OpenTK.Audio.OpenAL.AL.DeleteBuffers(bufferHandles);
		}

		public void BufferData(int bufferHandle, AudioFormat format, byte[] data, int length, int sampleRate)
		{
			global::OpenTK.Audio.OpenAL.AL.BufferData(bufferHandle, AudioFormatToALFormat(format), data, length, sampleRate);
		}

		public int CreateChannel()
		{
			return global::OpenTK.Audio.OpenAL.AL.GenSource();
		}

		public void DeleteChannel(int channelHandle)
		{
			global::OpenTK.Audio.OpenAL.AL.DeleteSource(channelHandle);
		}

		public void AttachBufferToChannel(int bufferHandle, int channelHandle)
		{
			global::OpenTK.Audio.OpenAL.AL.Source(channelHandle, global::OpenTK.Audio.OpenAL.ALSourcei.Buffer, bufferHandle);
		}

		public void QueueBufferInChannel(int bufferHandle, int channelHandle)
		{
			global::OpenTK.Audio.OpenAL.AL.SourceQueueBuffer(channelHandle, bufferHandle);
		}

		public int UnqueueBufferFromChannel(int channelHandle)
		{
			return global::OpenTK.Audio.OpenAL.AL.SourceUnqueueBuffer(channelHandle);
		}

		public int GetNumberOfBuffersQueued(int channelHandle)
		{
			int numberOfBuffersQueued;
			global::OpenTK.Audio.OpenAL.AL.GetSource(channelHandle, global::OpenTK.Audio.OpenAL.ALGetSourcei.BuffersQueued, out numberOfBuffersQueued);
			return numberOfBuffersQueued;
		}

		public int GetNumberOfBuffersProcessed(int channelHandle)
		{
			int numberOfBuffersProcessed;
			global::OpenTK.Audio.OpenAL.AL.GetSource(channelHandle, global::OpenTK.Audio.OpenAL.ALGetSourcei.BuffersProcessed, out numberOfBuffersProcessed);
			return numberOfBuffersProcessed;
		}

		public ChannelState GetChannelState(int channelHandle)
		{
			int sourceState;
			global::OpenTK.Audio.OpenAL.AL.GetSource(channelHandle, global::OpenTK.Audio.OpenAL.ALGetSourcei.SourceState, out sourceState);
			return ALSourceStateToChannelState((global::OpenTK.Audio.OpenAL.ALSourceState)sourceState);
		}

		public void SetVolume(int channelHandle, float volume)
		{
			global::OpenTK.Audio.OpenAL.AL.Source(channelHandle, global::OpenTK.Audio.OpenAL.ALSourcef.Gain, volume);
		}

		public void SetPosition(int channelHandle, Vector3D position)
		{
			global::OpenTK.Audio.OpenAL.AL.Source(channelHandle, global::OpenTK.Audio.OpenAL.ALSource3f.Position, position.X, position.Y, position.Z);
		}

		public void SetPitch(int channelHandle, float pitch)
		{
			global::OpenTK.Audio.OpenAL.AL.Source(channelHandle, global::OpenTK.Audio.OpenAL.ALSourcef.Pitch, pitch);
		}

		public void Play(int channelHandle)
		{
			global::OpenTK.Audio.OpenAL.AL.SourcePlay(channelHandle);
		}

		public void Stop(int channelHandle)
		{
			global::OpenTK.Audio.OpenAL.AL.SourceStop(channelHandle);
		}

		public bool IsPlaying(int channelHandle)
		{
			return global::OpenTK.Audio.OpenAL.AL.GetSourceState(channelHandle) == global::OpenTK.Audio.OpenAL.ALSourceState.Playing;
		}

		private static global::OpenTK.Audio.OpenAL.ALFormat AudioFormatToALFormat(AudioFormat audioFormat)
		{
			switch (audioFormat)
			{
				case AudioFormat.Mono8:
					return global::OpenTK.Audio.OpenAL.ALFormat.Mono8;
				case AudioFormat.Mono16:
					return global::OpenTK.Audio.OpenAL.ALFormat.Mono16;
				case AudioFormat.Stereo8:
					return global::OpenTK.Audio.OpenAL.ALFormat.Stereo8;
				default:
					return global::OpenTK.Audio.OpenAL.ALFormat.Stereo16;
			}
		}

		private static ChannelState ALSourceStateToChannelState(global::OpenTK.Audio.OpenAL.ALSourceState alSourceState)
		{
			switch (alSourceState)
			{
				case global::OpenTK.Audio.OpenAL.ALSourceState.Playing:
					return ChannelState.Playing;
				case global::OpenTK.Audio.OpenAL.ALSourceState.Paused:
					return ChannelState.Paused;
				default:
					return ChannelState.Stopped;
			}
		}
	}
}