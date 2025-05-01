using System.Collections.Generic;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;

namespace Modio
{
    /// <summary>
    /// The mod.io Terms of Use links. Each link contains a boolean field for if its required, Users must agree to all
    /// linked terms that have a True value for this field. <br/>
    /// Use <see cref="Get"/> to get the latest Terms of Use.
    /// </summary>
    public class TermsOfUse
    {
        public string TermsText { get; private set; }
        public string AgreeText { get; private set; }
        public string DisagreeText { get; private set; }
        public TermsOfUseLink[] Links { get; private set; }

        static Dictionary<string, TermsOfUse> _termsCache = new Dictionary<string, TermsOfUse>();

        /// <summary>
        /// Gets the latest Terms of Use from the mod.io API, or returns a cached one if it's already been received
        /// during the session.
        /// </summary>
        /// <returns><see cref="Error"/>.<see cref="Error.None"/> if successful. Otherwise, it will return the API error.</returns>
        public static async Task<(Error error, TermsOfUse result)> Get()
        {
            string langCode = ModioAPI.LanguageCodeResponse;
            
            if (_termsCache.TryGetValue(langCode, out TermsOfUse cachedTerms)) 
                return (Error.None, cachedTerms);

            (Error error, TermsObject? result) = await ModioAPI.Authentication.Terms();

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error getting Terms of Use: {error}");
                return (error, null);
            }
            
            // Casting here as it's nullable from API
            var termsObject = (TermsObject)result;
            
            TermsOfUse convertedTerms = ConvertTermsObjectToTermsOfUse(termsObject);
            _termsCache[langCode] = convertedTerms;

            return (Error.None, convertedTerms);
        }
        
        /// <summary>
        /// Gets a specific link from the received Terms of Use object.
        /// </summary>
        public TermsOfUseLink GetLink(LinkType type)
        {
            foreach (TermsOfUseLink link in Links)
                if (link.type == type) 
                    return link;

            // In theory, we shouldn't be able to hit this, something is very wrong if we do
            ModioLog.Error?.Log($"Could not find {type.ToString()} link in Terms of Use! The API may have changed!");
            return default(TermsOfUseLink);
        }

        static TermsOfUse ConvertTermsObjectToTermsOfUse(TermsObject termsObject)
        {
            var output = new TermsOfUse();

            output.TermsText = termsObject.Plaintext;
            output.AgreeText = termsObject.Buttons.Agree.Text;
            output.DisagreeText = termsObject.Buttons.Disagree.Text;
            
            var websiteLink = new TermsOfUseLink
            {
                type = LinkType.Website,
                text = termsObject.Links.Website.Text,
                url = termsObject.Links.Website.Url,
                required = termsObject.Links.Website.Required,
            };
            
            var termsLink = new TermsOfUseLink
            {
                type = LinkType.Terms,
                text = termsObject.Links.Terms.Text,
                url = termsObject.Links.Terms.Url,
                required = termsObject.Links.Terms.Required,
            };
            
            var privacyLink = new TermsOfUseLink
            {
                type = LinkType.Privacy,
                text = termsObject.Links.Privacy.Text,
                url = termsObject.Links.Privacy.Url,
                required = termsObject.Links.Privacy.Required,
            };
            
            var manageLink = new TermsOfUseLink
            {
                type = LinkType.Manage,
                text = termsObject.Links.Manage.Text,
                url = termsObject.Links.Manage.Url,
                required = termsObject.Links.Manage.Required,
            };

            output.Links = new[] { websiteLink, termsLink, privacyLink, manageLink, };

            return output;
        }
    }
    
    /// <summary>
    /// Represents a url as part of the TOS. The 'required' field can be used to determine whether
    /// it is a TOS requirement to be displayed to the end user when viewing the TOS text.
    /// </summary>
    public struct TermsOfUseLink
    {
        public LinkType type;
        public string text;
        public string url;
        public bool required;
    }

    public enum LinkType
    {
        Website,
        Terms,
        Privacy,
        Manage,
        /// <summary>This cannot be gotten from this endpoint! This is here to help band-aid CUI</summary>
        Refund,
    }
}

