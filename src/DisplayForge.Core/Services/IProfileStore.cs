using DisplayForge.Core.Models;

namespace DisplayForge.Core.Services;

public interface IProfileStore
{
    ProfileCollection LoadProfiles();

    void SaveProfiles(ProfileCollection collection);

    AppSettings LoadSettings();

    void SaveSettings(AppSettings settings);

    string DataDirectory { get; }
}
