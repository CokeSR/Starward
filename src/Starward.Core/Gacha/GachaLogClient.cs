using Microsoft.Win32;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using Starward.Core.Gacha.ZZZ;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace Starward.Core.Gacha;

public abstract class GachaLogClient
{


    protected const string WEB_CACHE_PATH_YS_CN = @"YuanShen_Data\webCaches\Cache\Cache_Data\data_2";
    protected const string WEB_CACHE_PATH_YS_OS = @"GenshinImpact_Data\webCaches\Cache\Cache_Data\data_2";

    protected const string API_PREFIX_YS_CN = "https://public-operation-hk4e.mihoyo.com/gacha_info/api/getGachaLog";
    protected const string API_PREFIX_YS_OS = "https://public-operation-hk4e-sg.hoyoverse.com/gacha_info/api/getGachaLog";

    protected static ReadOnlySpan<byte> SPAN_WEB_PREFIX_YS_CN => "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha-v3/index.html"u8;
    protected static ReadOnlySpan<byte> SPAN_WEB_PREFIX_YS_OS => "https://gs.hoyoverse.com/genshin/event/e20190909gacha-v3/index.html"u8;



    protected const string WEB_CACHE_SR_PATH = @"StarRail_Data\webCaches\Cache\Cache_Data\data_2";

    protected const string API_PREFIX_SR_CN = "https://public-operation-hkrpg.mihoyo.com/common/gacha_record/api/getGachaLog";
    protected const string API_PREFIX_SR_OS = "https://public-operation-hkrpg-sg.hoyoverse.com/common/gacha_record/api/getGachaLog";

    protected static ReadOnlySpan<byte> SPAN_WEB_PREFIX_SR_CN => "https://webstatic.mihoyo.com/hkrpg/event/e20211215gacha-v2/index.html"u8;
    protected static ReadOnlySpan<byte> SPAN_WEB_PREFIX_SR_OS => "https://gs.hoyoverse.com/hkrpg/event/e20211215gacha-v2/index.html"u8;


    protected const string WEB_CACHE_ZZZ_PATH = @"ZenlessZoneZero_Data\webCaches\Cache\Cache_Data\data_2";

    protected const string API_PREFIX_ZZZ_CN = "https://public-operation-nap.mihoyo.com/common/gacha_record/api/getGachaLog";
    protected const string API_PREFIX_ZZZ_OS = "https://public-operation-nap-sg.hoyoverse.com/common/gacha_record/api/getGachaLog";

    protected static ReadOnlySpan<byte> SPAN_WEB_PREFIX_ZZZ_CN => "https://webstatic.mihoyo.com/nap/event/e20230424gacha/index.html"u8;
    protected static ReadOnlySpan<byte> SPAN_WEB_PREFIX_ZZZ_OS => "https://gs.hoyoverse.com/nap/event/e20230424gacha/index.html"u8;





    protected const string REG_KEY_BH3_CN = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\崩坏3";
    protected const string REG_KEY_BH3_OS = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Honkai Impact 3";
    protected const string REG_KEY_BH3_GL = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Honkai Impact 3rd";
    protected const string REG_KEY_BH3_TW = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\崩壊3rd";
    protected const string REG_KEY_BH3_KR = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\붕괴3rd";
    protected const string REG_KEY_BH3_JP = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\崩壊3rd";




    protected readonly HttpClient _httpClient;


    public abstract IReadOnlyCollection<IGachaType> QueryGachaTypes { get; init; }



    public GachaLogClient(HttpClient? httpClient = null)
    {
        if (httpClient is null)
        {
            _httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });
        }
        else
        {
            _httpClient = httpClient;
        }
    }



    #region public method



    public async Task<long> GetUidByGachaUrlAsync(string gachaUrl)
    {
        var prefix = GetGachaUrlPrefix(gachaUrl);
        foreach (var gachaType in QueryGachaTypes)
        {
            var param = new GachaLogQuery(gachaType, 1, 1, 0);
            var list = await GetGachaLogByQueryAsync<GachaLogItem>(prefix, param);
            if (list.Count != 0)
            {
                return list.First().Uid;
            }
        }
        return 0;
    }



    public abstract Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, long endId = 0, string? lang = null, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default);


    public abstract Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, IGachaType gachaType, long endId = 0, string? lang = null, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default);


    public abstract Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaLogQuery query, CancellationToken cancellationToken = default);




    public static string? GetGachaUrlFromWebCache(GameBiz gameBiz, string? installPath = null)
    {
        var file = GetGachaCacheFilePath(gameBiz, installPath);
        if (File.Exists(file))
        {
            return FindMatchStringFromFile(file, GetGachaUrlPattern(gameBiz));
        }
        return null;
    }



    public static string GetGachaCacheFilePath(GameBiz gameBiz, string? installPath)
    {
        string file = gameBiz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => Path.Join(installPath, WEB_CACHE_PATH_YS_CN),
            GameBiz.hk4e_global => Path.Join(installPath, WEB_CACHE_PATH_YS_OS),
            GameBiz.hkrpg_cn or GameBiz.hkrpg_global or GameBiz.hkrpg_bilibili => Path.Join(installPath, WEB_CACHE_SR_PATH),
            GameBiz.nap_cn or GameBiz.nap_global or GameBiz.nap_bilibili => Path.Join(installPath, WEB_CACHE_ZZZ_PATH),
            _ => throw new ArgumentOutOfRangeException($"Unknown region {gameBiz}"),
        };
        DateTime lastWriteTime = DateTime.MinValue;
        if (File.Exists(file))
        {
            lastWriteTime = File.GetLastWriteTime(file);
        }
        string prefix = gameBiz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => @"YuanShen_Data\webCaches",
            GameBiz.hk4e_global => @"GenshinImpact_Data\webCaches",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_global or GameBiz.hkrpg_bilibili => @"StarRail_Data\webCaches",
            GameBiz.nap_cn or GameBiz.nap_global or GameBiz.nap_bilibili => @"ZenlessZoneZero_Data\webCaches",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {gameBiz}"),
        };
        string webCache = Path.Join(installPath, prefix);
        if (Directory.Exists(webCache))
        {
            foreach (var item in Directory.GetDirectories(webCache))
            {
                string target = Path.Join(item, @"Cache\Cache_Data\data_2");
                if (File.Exists(target) && File.GetLastWriteTime(target) > lastWriteTime)
                {
                    file = target;
                }
            }
        }
        return file;
    }



    private static ReadOnlySpan<byte> GetGachaUrlPattern(GameBiz gameBiz)
    {
        return gameBiz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => SPAN_WEB_PREFIX_YS_CN,
            GameBiz.hk4e_global => SPAN_WEB_PREFIX_YS_OS,
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => SPAN_WEB_PREFIX_SR_CN,
            GameBiz.hkrpg_global => SPAN_WEB_PREFIX_SR_OS,
            GameBiz.nap_cn or GameBiz.nap_bilibili => SPAN_WEB_PREFIX_ZZZ_CN,
            GameBiz.nap_global => SPAN_WEB_PREFIX_ZZZ_OS,
            _ => throw new ArgumentOutOfRangeException($"Unknown region {gameBiz}"),
        };
    }



    #endregion




    #region protected method



    protected async Task<T> CommonGetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        var wrapper = await _httpClient.GetFromJsonAsync(url, typeof(miHoYoApiWrapper<T>), GachaLogJsonContext.Default, cancellationToken) as miHoYoApiWrapper<T>;
        if (wrapper is null)
        {
            throw new miHoYoApiException(-1, "Response body is null");
        }
        else if (wrapper.Retcode != 0)
        {
            throw new miHoYoApiException(wrapper.Retcode, wrapper.Message);
        }
        else
        {
            return wrapper.Data;
        }
    }



    protected abstract string GetGachaUrlPrefix(string gachaUrl, string? lang = null);



    protected async Task<List<T>> GetGachaLogAsync<T>(string gachaUrl, long endId = 0, string? lang = null, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default) where T : GachaLogItem
    {
        endId = Math.Clamp(endId, 0, long.MaxValue);
        var prefix = GetGachaUrlPrefix(gachaUrl, lang);
        var result = new List<T>();
        foreach (var gachaType in QueryGachaTypes)
        {
            result.AddRange(await GetGachaLogByTypeAsync<T>(prefix, gachaType, endId, progress, cancellationToken));
        }
        return result;
    }




    protected async Task<List<T>> GetGachaLogAsync<T>(string gachaUrl, IGachaType gachaType, long endId = 0, string? lang = null, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default) where T : GachaLogItem
    {
        endId = Math.Clamp(endId, 0, long.MaxValue);
        var prefix = GetGachaUrlPrefix(gachaUrl, lang);
        return await GetGachaLogByTypeAsync<T>(prefix, gachaType, endId, progress, cancellationToken);
    }




    protected virtual async Task<List<T>> GetGachaLogByQueryAsync<T>(string gachaUrlPrefix, GachaLogQuery param, CancellationToken cancellationToken = default) where T : GachaLogItem
    {
        await Task.Delay(Random.Shared.Next(200, 300), cancellationToken);
        var url = $"{gachaUrlPrefix}&{param}";
        var wrapper = await _httpClient.GetFromJsonAsync(url, typeof(miHoYoApiWrapper<GachaLogResult<T>>), GachaLogJsonContext.Default, cancellationToken) as miHoYoApiWrapper<GachaLogResult<T>>;
        if (wrapper is null)
        {
            return new List<T>();
        }
        else if (wrapper.Retcode != 0)
        {
            throw new miHoYoApiException(wrapper.Retcode, wrapper.Message);
        }
        else
        {
            return wrapper.Data.List;
        }
    }




    protected async Task<List<T>> GetGachaLogByTypeAsync<T>(string prefix, IGachaType gachaType, long endId = 0, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default) where T : GachaLogItem
    {
        var param = new GachaLogQuery(gachaType, 1, 20, 0);
        var result = new List<T>();
        while (true)
        {
            progress?.Report((gachaType, param.Page));
            var list = await GetGachaLogByQueryAsync<T>(prefix, param, cancellationToken);
            result.AddRange(list);
            if (list.Count == 20 && list.Last().Id > endId)
            {
                param.Page++;
                param.EndId = list.Last().Id;
            }
            else
            {
                break;
            }
        }
        return result;
    }


    [SupportedOSPlatform("windows")]
    protected static string? GetGameInstallPathFromRegistry(string regKey)
    {
        var launcherPath = Registry.GetValue(regKey, "InstallPath", null) as string;
        var configPath = Path.Join(launcherPath, "config.ini");
        if (File.Exists(configPath))
        {
            var str = File.ReadAllText(configPath);
            var installPath = Regex.Match(str, @"game_install_path=(.+)").Groups[1].Value.Trim();
            if (Directory.Exists(installPath))
            {
                return Path.GetFullPath(installPath);
            }
        }
        return null;
    }


    protected static string? FindMatchStringFromFile(string path, ReadOnlySpan<byte> prefix)
    {
        using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var ms = new MemoryStream();
        fs.CopyTo(ms);
        var span = ms.ToArray().AsSpan();
        var index = span.LastIndexOf(prefix);
        if (index >= 0)
        {
            var length = span[index..].IndexOfAny("\0\""u8);
            return Encoding.UTF8.GetString(span.Slice(index, length));
        }

        return null;
    }


    #endregion




    #region Gacha Info


    public async Task<GenshinGachaWiki> GetGenshinGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        lang = LanguageUtil.FilterLanguage(lang);
        GenshinGachaWiki wiki;
        if (gameBiz.IsChinaServer() && lang is "zh-cn")
        {
            const string url = "https://api-takumi.mihoyo.com/event/platsimulator/config?gids=2&game=hk4e";
            wiki = await CommonGetAsync<GenshinGachaWiki>(url, cancellationToken);
        }
        else
        {
            string url = $"https://sg-public-api.hoyolab.com/event/simulatoros/config?lang={lang}";
            wiki = await CommonGetAsync<GenshinGachaWiki>(url, cancellationToken);
        }
        wiki.Language = lang;
        return wiki;
    }


    public async Task<StarRailGachaWiki> GetStarRailGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        lang = LanguageUtil.FilterLanguage(lang);
        StarRailGachaWiki wiki;
        if (gameBiz.IsChinaServer() && lang is "zh-cn")
        {
            const string url = "https://api-takumi.mihoyo.com/event/rpgsimulator/config?game=hkrpg";
            wiki = await CommonGetAsync<StarRailGachaWiki>(url, cancellationToken);
        }
        else
        {
            wiki = new StarRailGachaWiki { Game = "hkrpg", Avatar = new List<StarRailGachaInfo>(), Equipment = new List<StarRailGachaInfo>() };
            for (int i = 1; i <= 10; i++)
            {
                string url = $"https://sg-public-api.hoyolab.com/event/rpgcalc/avatar/list?game=hkrpg&lang={lang}&tab_from=TabAll&page={i}&size=100";
                var wrapper = await CommonGetAsync<StarRailGachaInfoWrapper>(url, cancellationToken);
                wiki.Avatar.AddRange(wrapper.List);
                if (wrapper.List.Count != 100)
                {
                    break;
                }
            }
            for (int i = 1; i <= 10; i++)
            {
                string url = $"https://sg-public-api.hoyolab.com/event/rpgcalc/equipment/list?game=hkrpg&lang={lang}&tab_from=TabAll&page={i}&size=100";
                var wrapper = await CommonGetAsync<StarRailGachaInfoWrapper>(url, cancellationToken);
                wiki.Equipment.AddRange(wrapper.List);
                if (wrapper.List.Count != 100)
                {
                    break;
                }
            }
        }
        wiki.Language = lang;
        return wiki;
    }


    public async Task<ZZZGachaWiki> GetZZZGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        lang = LanguageUtil.FilterLanguage(lang);
        ZZZGachaWiki wiki;
        if (gameBiz.IsChinaServer() && lang is "zh-cn")
        {
            const string url = $"https://starward.scighost.com/metadata/v1/zzz/ZZZGachaInfo.nap_cn.zh-cn.json";
            wiki = await CommonGetAsync<ZZZGachaWiki>(url, cancellationToken);
        }
        else
        {
            string url = $"https://starward.scighost.com/metadata/v1/zzz/ZZZGachaInfo.nap_global.{lang}.json";
            wiki = await CommonGetAsync<ZZZGachaWiki>(url, cancellationToken);
        }
        if (string.IsNullOrWhiteSpace(wiki.Language))
        {
            wiki.Language = lang;
        }
        return wiki;
    }



    #endregion



}
