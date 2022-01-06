﻿using System;
using System.Runtime.InteropServices;

using FFmpeg.AutoGen;

using Vortice.Direct3D11;

namespace FlyleafLib.MediaFramework.MediaRenderer
{
    public unsafe partial class Renderer
    {
        ID3D11Buffer psBuffer;
        PSBufferType psBufferData = new PSBufferType();
        float lastLumin;

        [StructLayout(LayoutKind.Sequential)]
        struct PSBufferType
        {
            // size needs to be multiple of 16

            public PSFormat format;
            public int coefsIndex;
            public HDRtoSDRMethod hdrmethod;

            public float brightness;
            public float contrast;

            public float g_luminance;
            public float g_toneP1;
            public float g_toneP2;
        }
        enum PSFormat : int
        {
            RGB     = 1,
            Y_UV    = 2,
            Y_U_V   = 3
        }
        public void UpdateContrast()
        {
            psBufferData.contrast = Config.Video.Contrast / 100.0f;
            context.UpdateSubresource(ref psBufferData, psBuffer);
            Present();
        }
        public void UpdateBrightness()
        {
            psBufferData.brightness = Config.Video.Brightness / 100.0f;
            context.UpdateSubresource(ref psBufferData, psBuffer);
            Present();
        }
        public void UpdateHDRtoSDR(AVMasteringDisplayMetadata* displayData = null, bool updateResource = true)
        {
            if (VideoDecoder.VideoStream == null || VideoDecoder.VideoStream.ColorSpace != "BT2020") return;

            float lumin = displayData == null || displayData->has_luminance == 0 ? lastLumin : displayData->max_luminance.num / (float)displayData->max_luminance.den;
            lastLumin = lumin;

            psBufferData.hdrmethod = Config.Video.HDRtoSDRMethod;

            if (psBufferData.hdrmethod == HDRtoSDRMethod.Reinhard)
            {
                psBufferData.g_toneP1 = lastLumin > 0 ? (float)(Math.Log10(100) / Math.Log10(lastLumin)) : 0.72f;
                if (psBufferData.g_toneP1 < 0.1f || psBufferData.g_toneP1 > 5.0f)
                    psBufferData.g_toneP1 = 0.72f;
            }
            else if (psBufferData.hdrmethod == HDRtoSDRMethod.Aces)
            {
                psBufferData.g_luminance = lastLumin > 0 ? lastLumin : 400.0f;
                psBufferData.g_toneP1 = Config.Video.HDRtoSDRTone;
            }
            else if (psBufferData.hdrmethod == HDRtoSDRMethod.Hable)
            {
                psBufferData.g_luminance = lastLumin > 0 ? lastLumin : 400.0f;
                psBufferData.g_toneP1 = 10000.0f / psBufferData.g_luminance * (2.0f / Config.Video.HDRtoSDRTone);
                psBufferData.g_toneP2 = psBufferData.g_luminance / (100.0f * Config.Video.HDRtoSDRTone);
            }

            context.UpdateSubresource(ref psBufferData, psBuffer);

            if (Control != null) Present();
        }
    }
}
