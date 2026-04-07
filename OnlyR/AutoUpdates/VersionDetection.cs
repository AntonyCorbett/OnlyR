using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using Serilog;

namespace OnlyR.AutoUpdates;

/// <summary>
/// Used to get the installed OnlyR version and the
/// latest OnlyR release version from the GitHub REST API.
/// </summary>
internal static class VersionDetection
{
    private static string LatestReleaseApiUrl => "https://api.github.com/repos/AntonyCorbett/OnlyR/releases/latest";

    [ExcludeFromCodeCoverage]
    private static string? GetLatestReleaseVersionString()
    {
        string? version = null;

        try
        {
#pragma warning disable U2U1025 // Avoid instantiating HttpClient
            using var client = new HttpClient();
#pragma warning restore U2U1025 // Avoid instantiating HttpClient

            client.DefaultRequestHeaders.UserAgent.ParseAdd("OnlyR");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var response = client.GetAsync(LatestReleaseApiUrl).Result;
            response.EnsureSuccessStatusCode();

            var content = response.Content.ReadAsStringAsync().Result;
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("tag_name", out var tagNameElement))
            {
                version = ExtractVersionFromTagName(tagNameElement.GetString());
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Getting latest release version");
        }

        return version;
    }

    public static string? ExtractVersionFromTagName(string? tagName) =>
        string.IsNullOrWhiteSpace(tagName) ? null : tagName.Trim().TrimStart('v', 'V');

    public static Version? GetLatestReleaseVersion()
    {
        var versionString = GetLatestReleaseVersionString();
        return ParseVersionString(versionString);
    }

    public static Version? ParseVersionString(string? versionString)
    {
        if (string.IsNullOrEmpty(versionString))
        {
            return null;
        }

        var tokens = versionString.Split('.');
        if (tokens.Length != 4)
        {
            Log.Logger.Error("Invalid version string format. Expected format: major.minor.build.revision. Value: {VersionString}", versionString);
            return null;
        }

        if (int.TryParse(tokens[0], out var major) &&
            int.TryParse(tokens[1], out var minor) &&
            int.TryParse(tokens[2], out var build) &&
            int.TryParse(tokens[3], out var revision))
        {
            if (major >= 0 && minor >= 0 && build >= 0 && revision >= 0)
            {
                return new Version(major, minor, build, revision);
            }

            Log.Logger.Error("Version string contains negative numbers {VersionString} ", versionString);
            return null;
        }

        Log.Logger.Error("Failed to parse version string as integers {VersionString} ", versionString);
        return null;
    }

    public static Version? GetCurrentVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version;
}