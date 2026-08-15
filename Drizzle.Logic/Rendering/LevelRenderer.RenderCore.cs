using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Drizzle.Lingo.Runtime;
using Drizzle.Ported;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Drizzle.Logic.Rendering;

public sealed partial class LevelRenderer
{
    private static readonly string PngSoftwareName = $"Drizzle {Assembly.GetExecutingAssembly().GetName().Version}";

    // This partial contains core rendering logic.
    private int _cameraIndex;
    private int _countCamerasDone;

    public void DoRender()
    {
        if (_singleCamera != null && _singleCamera.Value != 1)
            return; //only render camera 1
        if ((int)Movie.gCameraProps.cameras.count < 2)
            return; //only render multi-screen rooms

        RenderStart();

        // Single full-level pass: cameras are ignored entirely.
        RenderSetupCamera(0);
        RenderLayers();
        RenderPropsPreEffects();
        RenderEffects();
        RenderPropsPostEffects();
        RenderLight();
        RenderFinalize();
        RenderColors();
        RenderFinished();

        RenderStartFrame(RenderStage.SaveFile);

        var image = _runtime.GetCastMember("finalImage")!.image!;

        // === FLAT CROP TO CAMERA BOUNDING BOX ===========================================
        // finalImage maps matrix tile (1,1) -> pixel (0,0); camera positions live in the same
        // pixel space. Cropping the flat to [minCam .. maxCam + screen] reproduces exactly the
        // texture SBCameraScroll stitches from per-screen PNGs, so the mod can use its normal
        // min_camera_position (from the vanilla .txt cameras) with NO extra data shipped.
        // The editable extraTiles border drops out automatically (cameras never cover it).
        {
            const int CAM_OFFSET_X = 0;
            const int CAM_OFFSET_Y = 0;
            const int SCREEN_W = 1400;
            const int SCREEN_H = 800;

            var cams = Movie.gCameraProps.cameras;
            int nCams = (int)cams.count;
            if (nCams > 0)
            {
                int mnX = int.MaxValue, mnY = int.MaxValue, mxX = int.MinValue, mxY = int.MinValue;
                for (int i = 1; i <= nCams; i++)
                {
                    var cam = (dynamic)cams[i];
                    int cx = (int)cam.loch;
                    int cy = (int)cam.locv;
                    if (cx < mnX) mnX = cx;
                    if (cy < mnY) mnY = cy;
                    if (cx > mxX) mxX = cx;
                    if (cy > mxY) mxY = cy;
                }

                int fullW = (int)Movie.gLOprops.size.loch * 20;
                int fullH = (int)Movie.gLOprops.size.locv * 20;

                int minX = mnX + CAM_OFFSET_X;
                int minY = mnY + CAM_OFFSET_Y;
                int cropW = (mxX - mnX) + SCREEN_W;
                int cropH = (mxY - mnY) + SCREEN_H;

                if (minX < 0) minX = 0;
                if (minY < 0) minY = 0;
                if (minX > fullW - 1) minX = fullW - 1;
                if (minY > fullH - 1) minY = fullH - 1;
                if (cropW > fullW - minX) cropW = fullW - minX;
                if (cropH > fullH - minY) cropH = fullH - minY;
                if (cropW < 1) cropW = 1;
                if (cropH < 1) cropH = 1;

                if (minX != 0 || minY != 0 || cropW != fullW || cropH != fullH)
                {
                    var cropped = new LingoImage(cropW, cropH, 32);

                    cropped.copypixels(image, new LingoRect(0, 0, cropW, cropH), new LingoRect(minX, minY, minX + cropW, minY + cropH));
                    /* //MANUAL COPY METHOD
                    for (int y = 0; y < cropH; y++)
                        for (int x = 0; x < cropW; x++)
                            cropped.setpixel(x, y, image.getpixel(minX + x, minY + y));
                    */

                    //manually copy gDecalColors
                    LingoList decalColors = Movie.gDecalColors;
                    Console.WriteLine("decalColors.count = " + decalColors.count);
                    for (int i = 0; i < decalColors.count; i++)
                    {
                        cropped.setpixel(i, 0, decalColors[i + 1]);
                    }

                    image = cropped;
                    _runtime.GetCastMember("finalImage")!.image = cropped;

                    Log.Information(
                        "{LevelName} flat cropped to camera bbox: origin=({MinX},{MinY}) size={W}x{H} (full {FW}x{FH})",
                        Movie.gLoadedName, minX, minY, cropW, cropH, fullW, fullH);
                }
            }
        }
        // === END FLAT CROP ==============================================================

        OnScreenRenderCompleted?.Invoke(0, image);
        _countCamerasDone = 1;

        var fileName = Path.Combine(
            LingoRuntime.MovieBasePath, "Levels", $"{Movie.gLoadedName}_flat.png");
        Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);

        using (var file = File.Create(fileName))
        {
            var imgSharp = image.GetImgSharpImage();
            imgSharp.Metadata.GetPngMetadata().TextData.Add(
                new PngTextData("Software", PngSoftwareName, null, null));
            imgSharp.SaveAsPng(file);
        }

        //add an extra camera, but offset it by 30,000 pixels so that SBCameraScroll ordinarily discards it
        /* //UNUSED, camera cropping is used instead
        Movie.gCameraProps.cameras.Add(new LingoPoint(30000, 30000));
        LingoList tempList = new LingoList(new object[] { 0, 0 });
        Movie.gCameraProps.quads.add(new LingoList(new LingoList[] { tempList, tempList, tempList, tempList }));
        */

        //DON'T create the level file. It's not needed; we only need the .png
        //Movie.newmakelevel(Movie.gLoadedName);
    }


    private void RenderStart()
    {
        RenderStartFrame(RenderStage.Start);
        _runtime.CreateScript<renderStart>().exitframe();
    }

    private void RenderSetupCamera(int camIndex)
    {
        RenderStartFrame(RenderStage.CameraSetup);

        _cameraIndex = camIndex;
        Movie.gCurrentRenderCamera = new LingoNumber(camIndex);

        // Fixed pseudo-camera covering the whole level (15/10-tile margin).
        Movie.gRenderCameraTilePos = new LingoPoint(-15, -10);
        Movie.gRenderCameraPixelPos = new LingoPoint(0, 0);
    }

    private void RenderLayers()
    {
        int cols = (int)Movie.gLOprops.size.loch + 30;
        int rows = (int)Movie.gLOprops.size.locv + 20;

        RenderStartFrame(RenderStage.RenderLayers);

        for (var i = 0; i < 30; i++)
        {
            _runtime.GetCastMember($"layer{i}")!.image = new LingoImage(cols * 20, rows * 20, 32);
            _runtime.GetCastMember($"gradientA{i}")!.image = new LingoImage(cols * 20, rows * 20, ImageType.L8);
            _runtime.GetCastMember($"gradientB{i}")!.image = new LingoImage(cols * 20, rows * 20, ImageType.L8);
            _runtime.GetCastMember($"layer{i}dc")!.image = new LingoImage(cols * 20, rows * 20, 32);
        }

        _runtime.GetCastMember("rainBowMask")!.image = new LingoImage(cols * 20, rows * 20, 32);

        var sw = Stopwatch.StartNew();
        // Movie.gSkyColor = new LingoColor(0, 0, 0);
        Movie.gTinySignsDrawn = new LingoNumber(0);
        Movie.gRenderTrashProps = new LingoList();
        _runtime.GetCastMember(@"finalImage")!.image = new LingoImage(cols * 20, rows * 20, 32);
        _runtime.Global.the_randomSeed = Movie.gLOprops.tileseed;

        for (var i = 3; i > 0; i--)
        {
            // Don't measure pauses as part of the stopwatch.
            sw.Stop();
            RenderStartFrame(new RenderStageStatusLayers(i));
            sw.Start();

            if (ShouldSendPreview())
            {
                var images = new LingoImage[30];
                for (var j = 0; j < 30; j++)
                {
                    images[j] = _runtime.GetCastMember($"layer{j}")!.image!.DuplicateShared();
                }

                SendPreview(new RenderPreviewProps(images));
            }

            Movie.setuplayer(new LingoNumber(i));
        }

        Movie.gLastImported = "";
        Log.Information("{LevelName} rendered layers in {ElapsedMilliseconds} ms",
            Movie.gLoadedName, sw.ElapsedMilliseconds);

        Movie.c = new LingoNumber(1);
    }

    private void RenderPropsPreEffects()
    {
        RenderStartFrame(RenderStage.RenderPropsPreEffects);
        Movie.afterEffects = new LingoNumber(0);
        _runtime.CreateScript<renderPropsStart>().exitframe();

        var script = _runtime.CreateScript<renderProps>();
        while (Movie.keepLooping == 1)
        {
            if (ShouldSendPreview())
            {
                var images = new LingoImage[30];
                for (var j = 0; j < 30; j++)
                {
                    images[j] = _runtime.GetCastMember($"layer{j}")!.image!.DuplicateShared();
                }

                SendPreview(new RenderPreviewProps(images));
            }

            RenderStartFrame(RenderStage.RenderPropsPreEffects);
            script.newframe();
        }
    }

    private void RenderEffects()
    {
        RenderStartFrame(RenderStage.RenderEffects);
        _runtime.CreateScript<renderEffectsStart>().exitframe();

        var script = _runtime.CreateScript<renderEffects>();
        while (Movie.keepLooping == 1)
        {
            if (ShouldSendPreview())
            {
                var images = new LingoImage[30];
                for (var j = 0; j < 30; j++)
                {
                    images[j] = _runtime.GetCastMember($"layer{j}")!.image!.DuplicateShared();
                }

                SendPreview(new RenderPreviewEffects(
                    images,
                    _runtime.GetCastMember("blackOutImg1")!.image!,
                    _runtime.GetCastMember("blackOutImg2")!.image!));
            }

            var effectsList = (LingoList)Movie.gEEprops.effects;
            var effectNames = effectsList.List.Select(e => (string)((dynamic)e!).nm).ToArray();
            var totalCount = effectsList.List.Count;
            var curr = (int)Movie.r;
            var vert = (int)Movie.vertRepeater;
            RenderStartFrame(new RenderStageStatusEffects(totalCount, curr, vert, effectNames));
            script.newframe();
        }
    }

    private void RenderPropsPostEffects()
    {
        RenderStartFrame(RenderStage.RenderPropsPostEffects);
        Movie.afterEffects = new LingoNumber(1);
        _runtime.CreateScript<renderPropsStart>().exitframe();

        var script = _runtime.CreateScript<renderProps>();
        while (Movie.keepLooping == 1)
        {
            if (ShouldSendPreview())
            {
                var images = new LingoImage[30];
                for (var j = 0; j < 30; j++)
                {
                    images[j] = _runtime.GetCastMember($"layer{j}")!.image!.DuplicateShared();
                }

                SendPreview(new RenderPreviewProps(images));
            }

            RenderStartFrame(RenderStage.RenderPropsPostEffects);
            script.newframe();
        }

        // Can clear prop/tile LRU cache now since we won't use it from here on.
        Movie.ImageCacheClear();
    }

    private void RenderLight()
    {
        RenderStartFrame(RenderStage.RenderLight);
        _runtime.CreateScript<renderLightStart>().exitframe();

        if (Movie.gLOprops.light == new LingoNumber(0))
            return;

        var script = _runtime.CreateScript<renderLight>();
        while (Movie.keepLooping == 1)
        {
            if (ShouldSendPreview())
            {
                var images = new LingoImage[30];
                for (var j = 0; j < 30; j++)
                {
                    images[j] = _runtime.GetCastMember($"layer{j}sh")!.image!.DuplicateShared();
                }

                SendPreview(new RenderPreviewLights(images));
            }

            var curr = (int)Movie.c;
            RenderStartFrame(new RenderStageStatusLight(curr));
            script.newframe();
        }
    }

    private void RenderFinalize()
    {
        RenderStartFrame(RenderStage.Finalize);
        _runtime.CreateScript<finalize>().exitframe();
        _runtime.CreateScript<unify>().exitframe();
    }

    private void RenderColors()
    {
        var oldRenderColors = Environment.GetEnvironmentVariable("DRIZZLE_OLD_RENDER_COLORS") is not (null or "0");

        if (!oldRenderColors)
        {
            Log.Debug("Using new RenderColors");
            while (Movie.keepLooping == 1)
            {
                RenderStartFrame(RenderStage.RenderColors);
                RenderColorsNewFrame();
            }
        }
        else
        {
            Log.Debug("Using old RenderColors");
            var script = _runtime.CreateScript<renderColors>();
            while (Movie.keepLooping == 1)
            {
                RenderStartFrame(RenderStage.RenderColors);
                script.newframe();
            }
        }
    }

    private void RenderFinished()
    {
        RenderStartFrame(RenderStage.Finished);
        _runtime.CreateScript<finished>().exitframe();
    }
}
