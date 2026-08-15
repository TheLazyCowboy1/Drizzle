using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Drizzle.ConsoleApp;
using Drizzle.Lingo.Runtime;
using Drizzle.Lingo.Runtime.Utils;
using Drizzle.Logic;
using Drizzle.Logic.Rendering;
using Drizzle.Ported;
using Meow;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SixLabors.ImageSharp;

CultureFix.FixCulture();

//if (!CommandLineArgs.TryParse(args, out var parsedArgs))
//    return 1;
//rendered already: "CC", 
string[] REGIONS = new string[] { "SU" };
//List<string> fileList = REGIONS.SelectMany(
//            r => Directory.EnumerateFiles(Path.Combine(Assembly.GetEntryAssembly()!.Location, "..", "..", "..", "..", "..", "Data", "LevelEditorProjects", "World", r)))
string[] rooms = new string[] { "CL_B29", "CL_C03", "CL_C05", "CL_LCSWAP", "CL_LEDGE", "CL_LSCOREACCESS", "DM_I05", "DM_I06", "DM_I08", "DM_I09", "DM_I11", "DM_LAB11", "DM_LAB14", "DM_MEM01", "DM_MEM02", "DM_MEM04", "GW_B06", "GW_B06_PAST", "GW_B08", "GW_C04", "GW_C04_PAST", "GW_C05", "GW_C09", "HR_gbfi", "HR_L02", "HR_R04", "LC_02A", "LC_A02", "LC_C08", "LC_crash", "LC_fence", "LC_FINAL", "LC_longslum", "LC_mallentrance", "LC_ruin03", "LC_streets", "LC_stripmallNEW", "LC_stripmall", "LC_SUBWAY04", "LC_templegate", "LF_C01", "LF_C02", "LF_D02", "LF_D06", "LF_D08", "LF_D09", "LF_E01", "LF_E03", "LF_E04", "LF_E05", "LF_F01", "LF_H01", "LF_H02", "LF_J01", "LF_M01", "LF_M02", "LF_M04", "LF_Test3", "LM_B01", "LM_C01", "LM_C04", "LM_H02", "MS_B01", "MS_B04", "MS_D01", "MS_E01", "MS_HEART01", "MS_HEARTVENT01", "MS_HEARTVENT", "MS_I08", "MS_I11", "MS_LAB14", "MS_MEM01", "MS_MEM02", "MS_MEM04", "MS_splitsewers", "MS_V08", "MS_VENT03", "MS_VENT17", "OE_CAVE03", "RM_ASSEMBLY", "RM_C04", "RM_D02", "RM_DEAD01", "RM_DEAD03", "RM_E03", "RM_E05", "RM_LAB13", "RM_LAB3", "RM_LAB8", "RM_LC01", "RM_LC04", "RM_LCEXTRA", "RM_LCFILTERS", "RM_LCFINAL", "RM_LCHEADER", "RM_LCMANUFOLD", "RM_LCMEMEXIT", "RM_LCRPIPE", "RM_LCSWAP", "RM_LCTPIPE", "RM_LSCOREACCESS", "RM_LSLOCKDOWN", "RM_LSSECRET", "RM_LSVALVES", "SB_F03", "SB_H02", "SH_B12", "SH_C02_bkp", "SH_C02", "SH_C03", "SH_D03", "SH_E01RIV", "SH_E01", "SH_E02", "SH_E03RIV", "SH_E03", "SH_H01RIV", "SH_H01", "SH_LEDGE", "SH_OVERHEAD", "SI_B02x", "SI_B07x", "SI_B07", "SI_B09", "SI_C01x", "SI_C01", "SI_C02", "SI_C06x", "SI_C09", "SI_D01", "SI_D09", "SL_C04", "SL_C07", "SL_F02", "SS_C04", "SS_C08", "SS_D02", "SS_E03", "SS_E05", "trains", "UG_B07", "UG_D01", "UW_D04", "wara_P07", "WARA_P20", "WARC_A02", "WARC_A03", "WARC_A04", "WARC_A05", "WARC_A06", "WARC_A07", "WARC_B02", "WARC_B05", "WARC_B07", "WARC_B08", "WARC_B10", "WARC_B12", "WARC_C01", "WARC_C02", "WARC_C03", "WARC_C05", "WARC_C06", "WARC_C09", "WARC_E05", "WARC_E06", "WARC_F05", "WARC_F11", "ware_H02", "ware_H05", "ware_I03", "ware_i04", "ware_I09", "ware_I11", "warf_B32", "warg_B37", "warg_O13_Future", "WBLA_B02", "WBLA_B05", "WBLA_C03", "WBLA_F01", "WBLA_F02", "WBLA_F03", "WBLA_H01", "WBLA_J01", "wdsr_B07", "wgwr_C09b", "wgwr_C09", "wmpa_a04", "wmpa_a06", "wmpa_a07", "wmpa_a08", "wmpa_b01", "wmpa_b04", "WRFB_B01", "WRFB_B05", "WRFB_B06", "WRFB_B11", "WRFB_C07", "WRFB_C10", "WRFB_C13", "WRFB_D01", "WRFB_D02", "WRFB_D03", "WRFB_D04", "WRFB_F03", "wska_d22", "wskc_a10BG", "wskc_a10", "wskc_a14BG", "wskc_a14", "wskc_a15BG", "wskc_a15", "wskd_b01", "wskd_b02", "wskd_b12", "wskd_b28", "wskd_b33", "wskd_b38_bkg", "wskd_b38", "wssr_abyss", "wssr_LABBIG", "wssr_portholes" };
//string[] rooms = new string[] { "CL_B29" };
List<string> fileList = Directory.EnumerateFiles(Path.Combine(Assembly.GetEntryAssembly()!.Location, "..", "..", "..", "..", "..", "Data", "LevelEditorProjects", "World"), "*", SearchOption.AllDirectories)
    //.SelectMany(d => Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories)
        .Where(f => {
            if (!f.EndsWith(".txt")) return false;
            if (!rooms.Any(r => (r + ".txt").Equals(Path.GetFileName(f), StringComparison.InvariantCultureIgnoreCase))) return false;
            string path = Path.Combine(Assembly.GetEntryAssembly()!.Location, "..", "..", "..", "..", "..", "Data", "Levels", $"{Path.GetFileNameWithoutExtension(f)}_flat.png");
            return !File.Exists(path);// || File.GetCreationTimeUtc(f) > File.GetCreationTimeUtc(path);
            })
        .ToList();

var parsedArgs = new CommandLineArgs(new CommandLineArgs.VerbRender(4, fileList, false, null));

var isCi = Environment.GetEnvironmentVariable("CI") == "true";
var checksumErrors = 0;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Error()
    .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
    .CreateLogger();

Console.WriteLine("Rooms to render: " + string.Join(", ", fileList.Select(f => Path.GetFileNameWithoutExtension(f))));

return parsedArgs.Verb switch
{
    CommandLineArgs.VerbRender render => DoCmdRender(render),
    _ => throw new ArgumentOutOfRangeException()
};

int DoCmdRender(CommandLineArgs.VerbRender options)
{
    Configuration.Default.PreferContiguousImageBuffers = true;

    Console.WriteLine("Initializing Zygote runtime");

    var zygote = MakeZygoteRuntime();

    Console.WriteLine($"Starting render of {options.Levels.Count} levels");
    var sw = Stopwatch.StartNew();


    var errors = 0;
    var success = 0;

    var parallelOptions = new ParallelOptions
    {
        MaxDegreeOfParallelism = options.MaxParallelism == 0 ? -1 : options.MaxParallelism
    };

    var doChecksums = options.Checksums;
    Dictionary<string, Dictionary<string, string>>? checksums = null;
    if (options.CompareChecksums is { } chkFileName)
    {
        doChecksums = true;
        using var chkFile = File.OpenRead(chkFileName);
        checksums = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(chkFile);
    }

    Shuffle(options.Levels, new Random());

    Parallel.ForEach(options.Levels, parallelOptions, s =>
    {
        var renderRuntime = zygote.Clone();

        var levelName = Path.GetFileNameWithoutExtension(s);

        var levelSw = Stopwatch.StartNew();
        try
        {
            EditorRuntimeHelpers.RunLoadLevel(renderRuntime, s);

            var renderer = new LevelRenderer(renderRuntime, null);
            if (doChecksums)
                renderer.OnScreenRenderCompleted += (cam, img) => HandleChecksum(levelName, cam, img, checksums);

            renderer.DoRender();
        }
        catch (Exception e)
        {
            // Fancy error output for Actions CI.
            if (isCi)
                Console.WriteLine($"::error::{levelName}: Rendering failed");

            Console.WriteLine($"{levelName}: Exception while rendering!");
            Console.WriteLine(e);
            Interlocked.Increment(ref errors);
            return;
        }

        Console.WriteLine($"{levelName}: Render succeeded in {levelSw.Elapsed}");
        Interlocked.Increment(ref success);
    });

    Console.WriteLine($"Finished rendering in {sw.Elapsed}. {errors} errored, {success} succeeded");
    if (checksums != null)
        Console.WriteLine($"{checksumErrors} checksum failures.");

    return errors != 0 || checksumErrors != 0 ? 1 : 0;
}

void HandleChecksum(string name, int cameraIndex, LingoImage finalImg,
    Dictionary<string, Dictionary<string, string>>? checksums)
{
    Span<byte> hash = stackalloc byte[16];
    CalcChecksum(finalImg, hash);
    var hashHex = Convert.ToHexString(hash);

    Console.WriteLine($"checksum {name} cam {cameraIndex}: {hashHex}");
    if (checksums == null)
        return;

    if (!checksums.TryGetValue(name, out var cameras) ||
        !cameras.TryGetValue(cameraIndex.ToString(), out var checksum))
    {
        Console.WriteLine(
            $"{(isCi ? "::notice::" : "")}{name}#{cameraIndex} not found in checksum manifest.");
        return;
    }

    if (checksum != hashHex)
    {
        Console.WriteLine(
            $"{(isCi ? "::error::" : "")}{name}#{cameraIndex} mismatches checksum! New: {hashHex} old: {checksum}.");

        Interlocked.Increment(ref checksumErrors);
    }
    else
    {
        Console.WriteLine($"{name}#{cameraIndex}: Checksums passed");
    }
}

static void CalcChecksum(LingoImage img, Span<byte> outData)
{
    unsafe
    {
        Debug.Assert(sizeof(Vector128<byte>) == outData.Length);
    }

    if (img.depth.IntValue == 1)
    {
        // 1-bit images have padding bytes to make certain ops easier.
        // These bytes can contain undefined garbage,
        // and I can't be bothered to clear them to make sure the hash is consistent.
        // Just make it not supported for now, it's fine for the final images (those are 32bpp).
        throw new NotSupportedException();
    }

    var hash = MeowHash.Hash(MeowHash.MeowDefaultSeed, img.ImageBufferNoPadding);
    Unsafe.WriteUnaligned(ref outData[0], hash);
}

static LingoRuntime MakeZygoteRuntime()
{
    var runtime = new LingoRuntime(typeof(MovieScript).Assembly);
    runtime.Init();

    EditorRuntimeHelpers.RunStartup(runtime);

    return runtime;
}

static void Shuffle<T>(List<T> array, System.Random random)
{
    var n = array.Count;
    while (n > 1)
    {
        n--;
        var k = random.Next(n + 1);
        (array[k], array[n]) =
            (array[n], array[k]);
    }
}
