using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Mods;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Servers;
using GmlCore.Interfaces.System;
using GmlCore.Interfaces.User;

namespace GmlCore.Interfaces.Launcher;

public interface IGameProfile : IDisposable
{
    /// <summary>
    ///     Responsible for handling profile-specific operations.
    /// </summary>
    [JsonIgnore]
    IProfileProcedures ProfileProcedures { get; set; }

    /// <summary>
    ///     Responsible for server-specific operations related to the profile.
    /// </summary>
    [JsonIgnore]
    IProfileServersProcedures ServerProcedures { get; set; }

    /// <summary>
    ///     Manages game downloading operations.
    /// </summary>
    [JsonIgnore]
    IGameDownloaderProcedures GameLoader { get; set; }

    /// <summary>
    ///     Name of the game profile.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    ///     Indicates if the game profile is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    ///     Version of the game.
    /// </summary>
    string GameVersion { get; set; }

    /// <summary>
    ///     Version of the game at launch.
    /// </summary>
    string? LaunchVersion { get; set; }

    /// <summary>
    ///     Game loader associated with the profile.
    /// </summary>
    GameLoader Loader { get; }

    /// <summary>
    ///     Path to the game client.
    /// </summary>
    string ClientPath { get; set; }

    /// <summary>
    ///     Base64 encoded icon for the profile.
    /// </summary>
    string? IconBase64 { get; set; }

    /// <summary>
    ///     Key for the background image.
    /// </summary>
    string? BackgroundImageKey { get; set; }

    /// <summary>
    ///     Description of the game profile.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    ///     List of files permitted by the profile.
    /// </summary>
    List<IFileInfo>? FileWhiteList { get; set; }

    /// <summary>
    ///     List of folders permitted by the profile.
    /// </summary>
    List<IFolderInfo>? FolderWhiteList { get; set; }

    /// <summary>
    ///     List of user GUIDs permitted by the profile.
    /// </summary>
    List<string> UserWhiteListGuid { get; set; }

    /// <summary>
    ///     List of servers associated with the profile.
    /// </summary>
    List<IProfileServer> Servers { get; }

    /// <summary>
    ///     Represents a collection of optional mods that can be selected to enhance or customize the game experience.
    /// </summary>
    List<IMod> OptionalMods { get; }

    /// <summary>
    ///     Represents a collection of core modifications or modules applicable to the game profile.
    /// </summary>
    List<IMod> Mods { get; }

    /// <summary>
    ///     Date and time when the profile was created.
    /// </summary>
    DateTimeOffset CreateDate { get; }

    /// <summary>
    ///     JVM arguments for the game.
    /// </summary>
    string? JvmArguments { get; set; }

    /// <summary>
    ///     Game arguments used at runtime.
    /// </summary>
    string? GameArguments { get; set; }

    /// <summary>
    ///     Current state of the game profile.
    /// </summary>
    ProfileState State { get; set; }

    /// <summary>
    ///     Represents the display name of a game profile, typically used for
    ///     identifying or labeling the profile in user interfaces or descriptions.
    /// </summary>
    string DisplayName { get; set; }

    /// <summary>
    ///     Indicates whether the profile can be modified.
    /// </summary>
    bool CanEdit { get; }

    /// <summary>
    ///     Represents the execution priority of the game profile, which determines its order or importance
    ///     when being processed or invoked within the application.
    /// </summary>
    int Priority { get; set; }

    /// <summary>
    ///     Represents the recommended amount of RAM, in megabytes, for optimal performance within the profile configuration.
    /// </summary>
    int RecommendedRam { get; set; }

    /// <summary>
    /// Specifies the relative path of the game profile within its directory structure.
    /// </summary>
    string ReleativePath { get; set; }

    ProfileJavaVendor JavaVendor { get; set; }
    string? JavaMajorVersion { get; set; }

    /// <summary>
    ///     Validates the game profile.
    /// </summary>
    Task<bool> ValidateProfile();

    /// <summary>
    ///     Checks if the profile is fully loaded.
    /// </summary>
    Task<bool> CheckIsFullLoaded(IStartupOptions startupOptions);

    /// <summary>
    ///     Removes the game profile.
    /// </summary>
    Task Remove();

    /// <summary>
    ///     Initiates the download process for the game.
    /// </summary>
    Task DownloadAsync();

    /// <summary>
    ///     Creates a process for the game.
    /// </summary>
    Task<Process> CreateProcess(IStartupOptions startupOptions, IUser user);

    /// <summary>
    ///     Checks if the game client exists.
    /// </summary>
    Task<bool> CheckClientExists();

    /// <summary>
    ///     Checks if the operating system type is loaded.
    /// </summary>
    Task<bool> CheckOsTypeLoaded(IStartupOptions startupOptions);

    /// <summary>
    ///     Installs authentication libraries.
    /// </summary>
    Task<string[]> InstallAuthLib();

    /// <summary>
    ///     Retrieves cached profile information.
    /// </summary>
    Task<IGameProfileInfo?> GetCacheProfile();

    /// <summary>
    ///     Adds a server to the profile.
    /// </summary>
    void AddServer(IProfileServer server);

    /// <summary>
    ///     Removes a server from the profile.
    /// </summary>
    void RemoveServer(IProfileServer server);

    /// <summary>
    ///     Creates a mods folder for the profile.
    /// </summary>
    Task CreateModsFolder();

    /// <summary>
    ///     Retrieves profile files based on operating system details.
    /// </summary>
    Task<ICollection<IFileInfo>> GetProfileFiles(string osName, string osArchitecture);

    /// <summary>
    ///     Retrieves information about a specific profile file within the specified directory.
    /// </summary>
    /// <param name="directory">The directory where the profile file is located.</param>
    /// <returns>An object representing the file information of the requested profile file, or null if the file does not exist.</returns>
    Task<IFileInfo?> GetProfileFiles(string directory);

    /// <summary>
    ///     Retrieves all profile files, optionally restoring from cache.
    /// </summary>
    Task<IFileInfo[]> GetAllProfileFiles(bool needRestoreCache);

    /// <summary>
    ///     Creates a user session asynchronously.
    /// </summary>
    Task CreateUserSessionAsync(IUser user);

    /// <summary>
    ///     Retrieves the mods associated with the game profile.
    /// </summary>
    Task<IReadOnlyCollection<IMod>> GetModsAsync();

    /// <summary>
    ///     Retrieves the optional mods associated with a game profile.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of optional mods.</returns>
    Task<IEnumerable<IMod>> GetOptionalsModsAsync();

    /// <summary>
    ///     Adds a mod to the game profile using the provided file name and stream data.
    /// </summary>
    /// <param name="fileName">The name of the mod file to be added.</param>
    /// <param name="streamData">The stream containing the data of the mod file.</param>
    /// <returns>An instance of <see cref="IMod" /> representing the added mod.</returns>
    Task<IMod> AddMod(string fileName, Stream streamData);

    /// <summary>
    ///     Adds an optional mod to the game profile. This allows the user
    ///     to include a mod that is not required for the core functionality
    ///     of the game but can enhance or expand the game's experience.
    /// </summary>
    /// <param name="fileName">The name of the mod file.</param>
    /// <param name="streamData">The stream containing the mod file's data.</param>
    /// <returns>Returns the added optional mod as an <see cref="IMod" /> instance.</returns>
    Task<IMod> AddOptionalMod(string fileName, Stream streamData);

    /// <summary>
    ///     Removes a specified mod from the game profile.
    /// </summary>
    /// <param name="modName">The name of the mod to be removed.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a boolean indicating whether the
    ///     mod was successfully removed.
    /// </returns>
    Task<bool> RemoveMod(string modName);

    /// <summary>
    ///     Sets the state of the game profile.
    /// </summary>
    /// <param name="state">
    ///     The desired state to set for the game profile. Refer to <see cref="ProfileState" /> for possible
    ///     values.
    /// </param>
    /// <returns>A task that represents the asynchronous operation of setting the state.</returns>
    Task SetState(ProfileState state);

    /// <summary>
    ///     Determines whether mods can be loaded for the game profile.
    /// </summary>
    /// <returns>
    ///     A task representing the asynchronous operation. The task result contains a boolean value indicating whether
    ///     mods can be loaded.
    /// </returns>
    Task<bool> CanLoadMods();
}
