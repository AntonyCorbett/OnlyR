using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Reflection;
using Serilog;

namespace OnlyR.AutoUpdates;

/// <summary>
/// Used to get the installed OnlyR version and the
/// latest OnlyR release version from the github webpage.
/// </summary>
internal static class VersionDetection
{
    public static string LatestReleaseUrl => "https://github.com/AntonyCorbett/OnlyR/releases/latest";

    [ExcludeFromCodeCoverage]
    public static string? GetLatestReleaseVersionString()
    {
        try
        {
#pragma warning disable U2U1025 // Avoid instantiating HttpClient
            using var client = new HttpClient();
#pragma warning restore U2U1025 // Avoid instantiating HttpClient

            var response = client.GetAsync(LatestReleaseUrl).Result;
            if (response.IsSuccessStatusCode)
            {
                return ExtractVersionFromUri(response.RequestMessage?.RequestUri);
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Getting latest release version");
        }

        return null;
    }

    public static string? ExtractVersionFromUri(Uri? uri)
    {
        if (uri == null)
        {
            return null;
        }

        var segments = uri.Segments;
        if (segments.Length == 0)
        {
            return null;
        }

        return segments[^1];
    }

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
            return null;
        }

        if (!int.TryParse(tokens[0], out var major) ||
            !int.TryParse(tokens[1], out var minor) ||
            !int.TryParse(tokens[2], out var build) ||
            !int.TryParse(tokens[3], out var revision))
        {
            return null;
        }

        return new Version(major, minor, build, revision);
    }

    public static Version? GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version;
    }
}