﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Terrascape.Debugging;
using Terrascape.Exceptions;
using Terrascape.Registry;

#nullable enable

namespace Terrascape.Rendering
{
	public class Texture : GraphicsObject
	{
		public readonly double width, half_width;
		public readonly double height, half_height;
		
		private Texture(Identifier p_name, int p_id, int p_width, int p_height) : base(p_name, p_id)
		{
			Debug.LogDebug($"Created Texture '{p_name}' ({p_id})");
			this.width = p_width;
			this.height = p_height;
			this.half_width = this.width / 2f;
			this.half_height = this.height / 2f;
		}

		internal void Use(TextureUnit p_unit = TextureUnit.Texture0)
		{
			GL.ActiveTexture(p_unit);
			GL.BindTexture(TextureTarget.Texture2D, this.ID);
		}
		
		protected override void Delete()
		{
			GL.DeleteTexture(this.ID);
		}
		
		[SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
		public static Texture Load(Identifier p_name, in string p_filename,
		                           TextureMinFilter p_minification_filter = TextureMinFilter.Linear, TextureMagFilter p_magnification_filter = TextureMagFilter.Linear,
		                           TextureWrapMode p_wrap_mode = TextureWrapMode.ClampToEdge)
		{
			string filename = $"{Directory.GetCurrentDirectory()}/Assets/Textures/{(p_filename.EndsWith(".png") ? p_filename : $"{p_filename}.png")}";
			if (!File.Exists(filename))
				throw new TerrascapeException($"Cannot load texture file '{filename}'"); // TODO(LOGIX): Custom exception type
			
			if (!(Image.Load(filename) is Image<Rgba32> image))
				throw new TerrascapeException("Failed to read texture"); // TODO(LOGIX): Custom exception type, better message

			Rgba32[]   temporary_pixels = image.GetPixelSpan().ToArray();
			List<byte> pixels           = new List<byte>();
			foreach (Rgba32 pixel in temporary_pixels)
			{
				pixels.Add(pixel.R);
				pixels.Add(pixel.G);
				pixels.Add(pixel.B);
				pixels.Add(pixel.A);
			}

			Texture texture = Create(p_name, pixels, image.Width, image.Height, p_minification_filter, p_magnification_filter, p_wrap_mode);
			return texture;
		}

		internal static Texture Create(Identifier p_name, in List<byte> p_data, in int p_image_width, in int p_image_height,
		                               TextureMinFilter p_minification_filter = TextureMinFilter.Linear, TextureMagFilter p_magnification_filter = TextureMagFilter.Linear,
		                               TextureWrapMode p_wrap_mode = TextureWrapMode.ClampToEdge, bool p_register = true)
		{
			// ReSharper disable once InconsistentNaming
			int         texture_ID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, texture_ID);
			
			GL.TexImage2D(TextureTarget.Texture2D,
				0, PixelInternalFormat.Rgba, p_image_width, p_image_height,
				0, PixelFormat.Rgba, PixelType.UnsignedByte, p_data.ToArray());

			// TODO(LOGIX): Store these in Texture?
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)p_minification_filter);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)p_magnification_filter);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)p_wrap_mode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)p_wrap_mode);

			// TODO(LOGIX): Look into
			/*
			// Enable anisotropic filtering to attempt to mitigate texel flickering
			GL.GetFloat((GetPName) OpenTK.Graphics.OpenGL.ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt, out float max_aniso);
			GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)OpenTK.Graphics.OpenGL.ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt, max_aniso);
			
			// Generate MipMap
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
			*/

			return new Texture(p_name, texture_ID, p_image_width, p_image_height);
		}
	}
}