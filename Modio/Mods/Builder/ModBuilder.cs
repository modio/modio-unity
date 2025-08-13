using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Caching;
using Modio.Errors;
using Modio.Users;

namespace Modio.Mods.Builder
{
    /// <summary>A programmatic interface for Creating a new <see cref="Mod"/>. This class will handle all the publishing
    /// tasks that need to be performed intelligently, minimizing the amount of requests made.</summary>
    /// <remarks>When editing a mod these properties will not be populated: <see cref="LogoFilePath"/>,
    /// <see cref="GalleryFilePaths"/>, <see cref="LogoFilePath"/></remarks>
    public class ModBuilder
    {
        /// <summary>Produces a list of Tuples with each published change &amp; their corresponding result.</summary>
        public List<(ChangeFlags, Error)> Results
            => _commitErrors?
               .Select(yeet => (yeet.Key, yeet.Value))
               .ToList();

        Dictionary<ChangeFlags, Error> _commitErrors;
        ChangeFlags _pendingChanges = ChangeFlags.None;

        public string Name { get; private set; } = null;
        public string Summary { get; private set; } = null;
        public string Description { get; private set; } = null;
        public string LogoFilePath { get; private set; } = null;
        byte[] _logoBytes;
        ImageFormat _logoBytesFormat;
        public string[] GalleryFilePaths { get; private set; } = null;
        bool _appendingGallery;

        public string[] Tags { get; private set; } = null;
        public string MetadataBlob { get; private set; } = null;
        public Dictionary<string, string> MetadataKvps { get; private set; } = null;
        
        ModfileBuilder _modfileBuilder = null;

        public List<long> Dependencies { get; private set; } = new List<long>();
        bool _appendingDependencies;
        
        public bool Visible { get; private set;  }
        public ModMaturityOptions MaturityOptions { get; private set; }
        public ModCommunityOptions CommunityOptions { get; private set; }

        MonetizationOptions _monetizationOptions;
        public bool IsMonetized { get; private set; }
        public bool IsLimitedStock { get; private set; }
        public int Price { get; private set; }
        public int Stock { get; private set; }

        public bool IsEditMode => EditTarget != null;
        public Mod EditTarget { get; private set; }

        internal ModBuilder() => EditTarget = null;

        internal ModBuilder(Mod editTarget)
        {
            EditTarget = editTarget;
            Name = editTarget.Name;
            Summary = editTarget.Summary;
            Description = editTarget.Description;
            Tags = editTarget.Tags.Select(tag => tag.ApiName).ToArray();
            MetadataBlob = editTarget.MetadataBlob;
            MetadataKvps = editTarget.MetadataKvps;

            _monetizationOptions = editTarget.IsMonetized
                ? MonetizationOptions.Enabled | MonetizationOptions.Live
                : MonetizationOptions.None;

            Price = editTarget.IsMonetized
                ? (int)editTarget.Price
                : 0;
        }

#region Builder Methods

        public ModBuilder SetName(string name)
        {
            Name = name;
            _pendingChanges |= ChangeFlags.Name;
            return this;
        }

        public ModBuilder SetSummary(string summary)
        {
            Summary = summary;
            _pendingChanges |= ChangeFlags.Summary;
            return this;
        }

        public ModBuilder SetDescription(string description)
        {
            Description = description;
            _pendingChanges |= ChangeFlags.Description;
            return this;
        }
        
        /// <remarks>This will overwrite all tags on the mod.</remarks>
        public ModBuilder SetTags(ICollection<string> tags)
        {
            Tags = tags.ToArray();
            _pendingChanges |= ChangeFlags.Tags;
            return this;
        }

        /// <remarks>This will overwrite all tags on the mod.</remarks>
        public ModBuilder SetTags(string tag) => SetTags(new[] { tag, });

        public ModBuilder AppendTags(ICollection<string> tags)
        {
            Tags = Tags.Concat(tags).ToArray();
            _pendingChanges |= ChangeFlags.Tags;
            return this;
        }

        public ModBuilder AppendTags(string tag) => AppendTags(new[] { tag, });

        public ModBuilder SetMetadataBlob(string data)
        {
            MetadataBlob = data;
            _pendingChanges |= ChangeFlags.MetadataBlob;
            return this;
        }
        
        public ModBuilder AppendMetadataBlob(string data)
        {
            MetadataBlob = string.Concat(MetadataBlob, data);
            _pendingChanges |= ChangeFlags.MetadataBlob;
            return this;
        }
        
        /// <remarks>This will overwrite any Key entered with the corresponding value.</remarks>
        public ModBuilder SetMetadataKvps(Dictionary<string, string> kvps)
        {
            foreach ((string key, string value) in kvps)
                MetadataKvps[key] = value;

            _pendingChanges |= ChangeFlags.MetadataKvps;
            return this;
        }

        public ModBuilder SetLogo(string logoFilePath)
        {
            LogoFilePath = logoFilePath;
            _pendingChanges |= ChangeFlags.Logo;
            return this;
        }

        public ModBuilder SetLogo(byte[] imageData, ImageFormat format)
        {
            _logoBytes = imageData;
            _logoBytesFormat = format;
            _pendingChanges |= ChangeFlags.Logo;
            return this;
        }

        /// <remarks>Will overwrite existing gallery images.</remarks>
        public ModBuilder SetGallery(ICollection<string> galleryImageFilePaths)
        {
            GalleryFilePaths = galleryImageFilePaths.ToArray();
            _appendingGallery = false;
            _pendingChanges |= ChangeFlags.Gallery;
            return this;
        }

        /// <remarks>Will overwrite existing gallery images.</remarks>
        public ModBuilder SetGallery(string galleryImageFilePath) => SetGallery(new[] { galleryImageFilePath, });

        public ModBuilder AppendGallery(ICollection<string> galleryImageFilePaths)
        {
            GalleryFilePaths = GalleryFilePaths.Concat(galleryImageFilePaths).ToArray();
            _appendingGallery = true;
            _pendingChanges |= ChangeFlags.Gallery;
            return this;
        }

        public ModBuilder AppendGallery(string galleryImageFilePath) => AppendGallery(new[] { galleryImageFilePath, });

        /// <remarks>Will overwrite existing dependencies.</remarks>
        public ModBuilder SetDependencies(ICollection<long> dependencies)
        {
            Dependencies = dependencies.ToList();
            _appendingDependencies = false;
            _pendingChanges |= ChangeFlags.Dependencies;
            return this;
        }

        /// <remarks>Will overwrite existing dependencies.</remarks>
        public ModBuilder SetDependencies(long dependency) => SetDependencies(new[] { dependency, });

        public ModBuilder AppendDependencies(ICollection<long> dependencies)
        {
            Dependencies = Dependencies.Concat(dependencies).ToList();
            _appendingDependencies = true;
            _pendingChanges |= ChangeFlags.Dependencies;
            return this;
        }

        public ModBuilder AppendDependencies(long dependency) => AppendDependencies(new[] { dependency, });

        public ModfileBuilder EditModfile()
        {
            _modfileBuilder ??= new ModfileBuilder(this);

            _pendingChanges |= ChangeFlags.Modfile;
            return _modfileBuilder;
        }

        public ModBuilder SetVisible(bool isVisible)
        {
            Visible = isVisible;
            _pendingChanges |= ChangeFlags.Visibility;
            return this;
        }

        public ModBuilder SetMaturityOptions(ModMaturityOptions maturityOptions)
        {
            MaturityOptions |= maturityOptions;
            _pendingChanges |= ChangeFlags.MaturityOptions;
            return this;
        }

        public ModBuilder OverwriteMaturityOptions(ModMaturityOptions maturityOptions)
        {
            MaturityOptions = maturityOptions;
            _pendingChanges |= ChangeFlags.MaturityOptions;
            return this;
        }

        public ModBuilder SetCommunityOptions(ModCommunityOptions communityOptions)
        {
            CommunityOptions |= communityOptions;
            _pendingChanges |= ChangeFlags.CommunityOptions;
            return this;
        }
        
        public ModBuilder OverwriteCommunityOptions(ModCommunityOptions communityOptions)
        {
            CommunityOptions = communityOptions;
            _pendingChanges |= ChangeFlags.CommunityOptions;
            return this;
        }

        public ModBuilder SetMonetized(bool isMonetized)
        {
            if (isMonetized)
                _monetizationOptions |= MonetizationOptions.Enabled | MonetizationOptions.Live;
            else
                _monetizationOptions &= ~(MonetizationOptions.Enabled | MonetizationOptions.Live);

            IsMonetized = isMonetized;
            
            _pendingChanges |= ChangeFlags.MonetizationConfig;
            return this;
        }

        public ModBuilder SetPrice(int price)
        {
            if (!_monetizationOptions.HasFlag(MonetizationOptions.Enabled | MonetizationOptions.Live))
            {
                ModioLog.Error?.Log("Mod is not set for Monetization! Use SetMonetized(bool isMonetized) before setting a price.");
                return this;
            }
            
            Price = price;
            
            _pendingChanges |= ChangeFlags.MonetizationConfig;
            return this;
        }

        public ModBuilder SetLimitedStock(bool isLimitedStock)
        {
            if (isLimitedStock)
                _monetizationOptions |= MonetizationOptions.LimitedStock;
            else
                _monetizationOptions &= MonetizationOptions.LimitedStock;

            IsLimitedStock = isLimitedStock;
            
            _pendingChanges |= ChangeFlags.MonetizationConfig;
            return this;
        }

        public ModBuilder SetStockAmount(int stockAmount)
        {
            if (!_monetizationOptions.HasFlag(
                    MonetizationOptions.Enabled 
                    | MonetizationOptions.Live 
                    | MonetizationOptions.LimitedStock)
               ) {
                ModioLog.Error?.Log("Mod is not set for Monetization or Limited Stock! Use SetMonetized(bool isMonetized) & SetLimtedStock(bool isLimitedStock) before setting a stock value.");
                return this;
            }
            
            Stock = stockAmount;
            _pendingChanges |= ChangeFlags.MonetizationConfig;
            return this;
        }

#endregion

#region Publish Methods

        /// <summary>Publishes the changes to the mod.io API.</summary>
        /// <remarks>Will use <see cref="ModioAPI.Mods.AddMod"/> or <see cref="ModioAPI.Mods.EditMod"/> to publish
        /// changes, then process the remaining data as separate publish tasks.</remarks>
        /// <returns><c>Error.None</c> if the initial request succeeds. Use <see cref="Results"/> to inspect the results
        /// of each publish task.</returns>
        public async Task<(Error error, Mod mod)> Publish()
        {
            _commitErrors = new Dictionary<ChangeFlags, Error>();
            return IsEditMode ? await PublishEdits() : await PublishNewMod();
        }

        async Task<(Error error, Mod mod)> PublishNewMod()
        {
            if (!_pendingChanges.HasFlag(
                    ChangeFlags.Name
                    | ChangeFlags.Summary
                    | ChangeFlags.Logo)
            ) {
                ModioLog.Error?.Log($"Can't publish mod [{Name}], mod must have the Name, Summary & Logo all filled before it can be added.");
                return (new Error(ErrorCode.BAD_PARAMETER), null);
            }

            string nameId = Name.ToLowerInvariant().Replace(' ', '-');

            (Error error, ModioAPIFileParameter logo) = TryGetLogoFileParameter();

            if (error)
            {
                ModioLog.Error?.Log($"Error getting File parameter from Logo: {error}");
                return (error, null);
            }

            var body = new AddModRequest(
                Name,
                nameId,
                Summary,
                Description,
                logo,
                _pendingChanges.HasFlag(ChangeFlags.Visibility) 
                    ? Visible ? 0 : 1 
                    : null,
                _pendingChanges.HasFlag(ChangeFlags.MaturityOptions) ? (long)MaturityOptions : null,
                _pendingChanges.HasFlag(ChangeFlags.CommunityOptions) ? (long)CommunityOptions : null,
                MetadataBlob,
                Tags
            );

            ModObject? modObject;
            (error, modObject) = await ModioAPI.Mods.AddMod(body);

            _commitErrors[ChangeFlags.AddFlags] = error;
            
            if (error || !modObject.HasValue)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error publishing new mod {Name}: {error}");
                return (error, null);
            }

            // We want to reset the change flags for the ones we uploaded
            // See notes for logic behind silly business here
            _pendingChanges &= ~ChangeFlags.AddFlags;

            Mod mod = ModCache.GetMod(modObject.Value);
            EditTarget = mod;

            await PublishRemainingChanges();
            await mod.GetModDetailsFromServer();
            
            // Ensure we're not caching searches without the new mod (especially on the /me endpoint)
            ModCache.ClearModSearchCache();
            
            return (Error.None, mod);
        }

        (Error error, ModioAPIFileParameter logo) TryGetLogoFileParameter()
        {
            ModioAPIFileParameter logo = ModioAPIFileParameter.None;
            Error error = Error.None;

            if (!string.IsNullOrEmpty(LogoFilePath))
            {
                (error, logo) = LogoFromFilePath(LogoFilePath);
                if (error) 
                    ModioLog.Error?.Log($"Couldn't create Logo file from file path {LogoFilePath}, cannot publish edits");
            }
            else if (_logoBytes != null && _logoBytes.Length > 0)
                logo = LogoFromByteArray();
            else
            {
                ModioLog.Error?.Log($"Couldn't create Logo file from either source! Cannot publish edits");
                error = new Error(ErrorCode.BAD_PARAMETER);
            }
                
            return (error, logo);
        }

        async Task PublishRemainingChanges()
        {
            List<ChangeFlags> remainingChanges = Enum.GetValues(typeof(ChangeFlags))
                                                     .Cast<ChangeFlags>()
                                                     .Where(flag => _pendingChanges.HasFlag(flag))
                                                     .Where(flag => flag != ChangeFlags.None)
                                                     .ToList();

            foreach (ChangeFlags change in remainingChanges)
            {
                Error error = await GetChangeSpecificPublishTask(change);

                _commitErrors[change] = error;
            }
        }

        async Task<(Error error, Mod mod)> PublishEdits()
        {
            if (_pendingChanges == ChangeFlags.None)
            {
                ModioLog.Error?.Log($"Can't publish changes for mod {EditTarget.Name}, no changes pending.");
                return (new Error(ErrorCode.BAD_PARAMETER), null);
            }
            
            string nameId = Name?.ToLowerInvariant().Replace(' ', '-');

            ModioAPIFileParameter logo = ModioAPIFileParameter.None;
            Error error;
            
            if (_pendingChanges.HasFlag(ChangeFlags.Logo))
            {
                (error, logo) = TryGetLogoFileParameter();

                if (error)
                {
                    ModioLog.Error?.Log($"Error getting File parameter from logo, cannot publish edits: {error}");
                    return (error, EditTarget);
                }
            }

            if((_pendingChanges & ChangeFlags.EditFlags) != 0)
            {
                var body = new EditModRequest(
                    _pendingChanges.HasFlag(ChangeFlags.Name) ? Name : null,
                    _pendingChanges.HasFlag(ChangeFlags.Name) ? nameId : null,
                    _pendingChanges.HasFlag(ChangeFlags.Summary) ? Summary : null,
                    _pendingChanges.HasFlag(ChangeFlags.Description) ? Description : null,
                    logo,
                    _pendingChanges.HasFlag(ChangeFlags.Visibility)
                        ? Visible ? 0 : 1
                        : null,
                    _pendingChanges.HasFlag(ChangeFlags.MaturityOptions) ? (long)MaturityOptions : null,
                    _pendingChanges.HasFlag(ChangeFlags.CommunityOptions) ? (long)CommunityOptions : null,
                    _pendingChanges.HasFlag(ChangeFlags.MetadataBlob) ? MetadataBlob : null,
                    _pendingChanges.HasFlag(ChangeFlags.Tags) ? Tags : null,
                    _pendingChanges.HasFlag(ChangeFlags.MonetizationConfig) ? (long)_monetizationOptions : null,
                    _pendingChanges.HasFlag(ChangeFlags.MonetizationConfig) ? Price : null,
                    _pendingChanges.HasFlag(ChangeFlags.MonetizationConfig) ? Stock : null
                );

                ModObject? modObject;
                (error, modObject) = await ModioAPI.Mods.EditMod(EditTarget.Id, body);

                _commitErrors[ChangeFlags.EditFlags] = error;

                if (error)
                {
                    if (!error.IsSilent)
                        ModioLog.Error?.Log($"Error publishing changes for mod {EditTarget.Name}: {error}");

                    return (error, EditTarget);
                }

                // We want to reset the change flags for the ones we uploaded
                // See notes for logic behind silly business here
                _pendingChanges &= ~ChangeFlags.EditFlags;

                EditTarget.ApplyDetailsFromModObject(modObject.Value);
            }
            await PublishRemainingChanges();
            await EditTarget.GetModDetailsFromServer();
            
            foreach ((ChangeFlags _, Error commitError) in _commitErrors)
            {
                if (commitError)
                    return (commitError, EditTarget);
            }

            return (Error.None, EditTarget);
        }

        async Task<Error> PublishGallery()
        {
            // Sync is inverse positive for appending, true = overwrite, false = append
            bool sync = !_appendingGallery;

            (Error error, ModioAPIFileParameter file) = await GalleryZipFromFilePaths(GalleryFilePaths);

            if (error)
            {
                ModioLog.Error?.Log($"Error creating {typeof(ModioAPIFileParameter)} for gallery publish request: {error}");
                return error;
            }
            
            var requestBody = new AddModMediaRequest(file, sync);
            (error, _) = await ModioAPI.Media.AddModMedia(EditTarget.Id, requestBody);

            if (error && !error.IsSilent)
                ModioLog.Error?.Log($"Error publishing Gallery for {EditTarget.Name}: {error}");
            
            return error;
        }

        async Task<Error> PublishMetadataKvps()
        {
            if (MetadataKvps is null)
            {
                ModioLog.Error?.Log($"Can't publish null MetadataKvps for mod {EditTarget.Name}");
                return new Error(ErrorCode.BAD_PARAMETER);
            }
            
            var body = new AddModMetadataRequest(MetadataKvps
                                                 .Select(kvp => $"{kvp.Key}:{kvp.Value}")
                                                 .ToArray());

            (Error error, _)
                = await ModioAPI.Metadata.AddModKvpMetadata(EditTarget.Id, body);

            if (error && !error.IsSilent)
                ModioLog.Error?.Log($"Error publishing MetadataKvps for {EditTarget.Name}: {error}");

            return error;
        }

        async Task<Error> PublishDependencies()
        {
            bool sync = !_appendingDependencies;

            var body = new AddModDependenciesRequest(Dependencies.ToArray(), sync);
            
            (Error error, _) 
                = await ModioAPI.Dependencies.AddModDependencies(EditTarget.Id, body);

            if (error && !error.IsSilent)
                ModioLog.Error?.Log($"Error publishing Dependencies for {EditTarget.Name}: {error}");

            return error;
        }

        Task<Error> PublishModfile() => _modfileBuilder.PublishModfile();

        async Task<Error> PublishMonetization()
        {
            Error error;
            
            var body = new EditModRequest(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                (long)_monetizationOptions,
                Price,
                Stock
            );

            (error, _) = await ModioAPI.Mods.EditMod(EditTarget.Id, body);

            if (error && !error.IsSilent)
                ModioLog.Error?.Log($"Error publishing Monetization changes for {EditTarget.Id}: {error}");

            return error;
        }

        /// <summary>
        /// Delete a mod on the mod.io backend. Note that this puts it into
        /// an 'archived' state, and mods can only be permanently deleted
        /// from the mod.io website
        /// </summary>
        public async Task<Error> ArchiveMod()
        {
            if (!IsEditMode)
            {
                ModioLog.Error?.Log($"Can't archive a mod that has never been published");
                return Error.Unknown;
            }

            (Error error, Response204? _) = await ModioAPI.Mods.DeleteMod(EditTarget.Id);

            if (error)
                return error;
            
            ModCache.ClearMod(EditTarget.Id);
            
            User.Current?.ModRepository?.RemoveMod(EditTarget);
            ModInstallationManagement.WakeUp();
            
            return error;
        }

#endregion

        Task<Error> GetChangeSpecificPublishTask(ChangeFlags flag) => flag switch
        {
            ChangeFlags.Gallery            => PublishGallery(),
            ChangeFlags.MetadataKvps       => PublishMetadataKvps(),
            ChangeFlags.Modfile            => PublishModfile(),
            ChangeFlags.MonetizationConfig => PublishMonetization(),
            ChangeFlags.Dependencies       => PublishDependencies(),
            // The below are all covered by the Add/Edit endpoints, so should never be called
            ChangeFlags.Name             => throw new ArgumentException($"{flag} should be changed through the Mods endpoint"),
            ChangeFlags.Summary          => throw new ArgumentException($"{flag} should be changed through the Mods endpoint"),
            ChangeFlags.Description      => throw new ArgumentException($"{flag} should be changed through the Mods endpoint"),
            ChangeFlags.Logo             => throw new ArgumentException($"{flag} should be changed through the Mods endpoint"),
            ChangeFlags.MetadataBlob     => throw new ArgumentException($"{flag} should be changed through the Mods endpoint"),
            ChangeFlags.Tags             => throw new ArgumentException($"{flag} should be changed through the Mods endpoint"),
            ChangeFlags.Visibility       => throw new ArgumentException($"{flag} should be changed through the Mods endpoint"),
            ChangeFlags.CommunityOptions => throw new ArgumentException($"{flag} should be changed through the Mods endpoint"),
            ChangeFlags.MaturityOptions  => throw new ArgumentException($"{flag} should be changed through the Mods endpoint"),
            ChangeFlags.AddFlags         => throw new ArgumentException($"{flag} should not be gotten from the {nameof(GetChangeSpecificPublishTask)} function! This could result in erroneous data being uploaded!"),
            ChangeFlags.EditFlags        => throw new ArgumentException($"{flag} should not be gotten from the {nameof(GetChangeSpecificPublishTask)} function! This could result in erroneous data being uploaded!"),
            ChangeFlags.None             => throw new ArgumentException("None changes?"),
            _                            => throw new ArgumentException($"Change flag {flag} doesn't exist!"),
        };

        static bool ValidateImageFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                ModioLog.Error?.Log($"Image file path {filePath} cannot be null or empty!");
                return false;
            }
            
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            string fileName = Path.GetFileName(filePath).ToLowerInvariant();

            if (string.IsNullOrEmpty(fileName))
            {
                ModioLog.Error?.Log($"Image file name {fileName} from path {filePath} cannot be null or empty!");
                return false;
            }

            if (extension != ".png"
                && extension != ".jpeg"
                && extension != ".jpg")
            {
                ModioLog.Error?.Log($"Invalid file extension: {extension}. It must be either a .png, .jpg or .jpeg.");
                return false;
            }

            if (!File.Exists(filePath))
            {
                ModioLog.Error?.Log($"Image {filePath} not found on file system.");
                return false;
            }

            return true;
        }

        ModioAPIFileParameter LogoFromByteArray() => new ModioAPIFileParameter(
            new MemoryStream(_logoBytes)
            {
                Position = 0,
            },
            $"logo.{_logoBytesFormat}",
            $"image/{_logoBytesFormat}"
        );

        static (Error error, ModioAPIFileParameter file) LogoFromFilePath(string filePath)
        {
            if (!ValidateImageFilePath(filePath))
                return (new Error(ErrorCode.BAD_PARAMETER), default(ModioAPIFileParameter));
            
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension)) 
                return (new Error(ErrorCode.BAD_PARAMETER), default(ModioAPIFileParameter));

            return (Error.None, new ModioAPIFileParameter(
                $"logo{extension}",
                $"image/{extension[1..]}",
                filePath
            ));
        }

        static async Task<(Error error, ModioAPIFileParameter file)> GalleryZipFromFilePaths(ICollection<string> imageFilePaths)
        {
            foreach (string filePath in imageFilePaths)
            {
                if (ValidateImageFilePath(filePath))
                    continue;

                ModioLog.Error?.Log($"Can't upload {imageFilePaths.Count} gallery images, {filePath} is invalid.");
                return (new Error(ErrorCode.BAD_PARAMETER), default(ModioAPIFileParameter));
            }

            var memStream = new MemoryStream();
            await using var zipStream = new ZipOutputStream(memStream);
            zipStream.IsStreamOwner = false;
            
            foreach (string imageFilePath in imageFilePaths)
            {
                string imageFileName = Path.GetFileName(imageFilePath);
                
                var newEntry = new ZipEntry(imageFileName);
                zipStream.PutNextEntry(newEntry);
                
                await using Stream readStream = File.Open(imageFilePath, FileMode.Open);
                await readStream.CopyToAsync(zipStream);
                
                zipStream.CloseEntry();
            }
            
            zipStream.Finish();

            memStream.Position = 0;

            return (Error.None, new ModioAPIFileParameter(memStream, "images.zip", "application/zip"));
        }
        
        [Flags]
        enum MonetizationOptions
        {
            None         = 0,
            Enabled      = 1,
            Live         = 2,
            LimitedStock = 8,
        }

        static string GetExtensionFromFormat(ImageFormat format) => format switch
        {
            ImageFormat.Jpeg => "jpeg",
            ImageFormat.Jpg  => "jpg",
            ImageFormat.Png  => "png",
            _                => throw new ArgumentException($"Image format {format} not supported!"),
        };
    }
    
    public enum ImageFormat
    {
        Jpg,
        Jpeg,
        Png,
    }
    
    /*
     * Murphy's Bitwise Notes for Walnut Brain Individuals (such as myself):
     *
     * & -> AND bit      -> 1 if BOTH target bits are 1, 0 otherwise
     * | -> OR bit       -> 1 if EITHER target bits are 1, 0 otherwise
     * ~ -> REVERSE bits -> all target bits are 0
     *
     * To Clear Flag:
     * By capturing the required flags and setting them to 0, the AND produces 0s at their positions
     */
}
