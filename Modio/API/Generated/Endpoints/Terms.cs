// <auto-generated />
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Modio.API.SchemaDefinitions;
using Modio.Errors;

namespace Modio.API
{
    public static partial class ModioAPI
    {
        public static partial class Authentication
        {
            /// <summary>
            /// <p>The purpose of this endpoint is to provide the text, links and buttons you can use to get a users agreement and consent prior to authenticating them in-game (your dialog should look similar to the example below). This text will be localized based on the `Accept-Language` header, into one of our [supported languages](#localization) (note: our full Terms of Use and Privacy Policy are currently in English-only). If you are authenticating using platform SSO, you must call this endpoint with the `X-Modio-Portal` [header set](#targeting-a-portal), so the text is localized to match the platforms requirements. A successful response will return a [Terms Object](#terms-object).</p>
            /// <p>__Example Dialog:__</p>
            /// <p><aside class='consent'>This game uses mod.io to support user-generated content. By clicking 'I Agree' you agree to the mod.io Terms of Use and a mod.io account will be created for you (using your display name, avatar and ID). Please see the mod.io Privacy Policy on how mod.io processes your personal data.<br/><br/><div style='text-align:center'><span class='versionwrap cursor'> &amp;nbsp; &amp;nbsp; I Agree &amp;nbsp; &amp;nbsp; </span> <span class='versionwrap outline cursor'>No, Thanks</span><br/><br/>[Terms of Use](https://mod.io/terms/widget) - [Privacy Policy](https://mod.io/privacy/widget)<br/></div></aside></p>
            /// <p>__IMPORTANT:__ It is a requirement of the [Game Terms](https://mod.io/gameterms) with mod.io, and the platforms mod.io is used on, to ensure the user provides consent and has agreed to the latest mod.io [Terms of Use](https://mod.io/terms/widget) and [Privacy Policy](https://mod.io/privacy/widget). The users agreement must be collected prior to using a 3rd party authentication flow (including but not limited to Steam, PSN, Nintendo and Xbox Live). You only need to collect the users agreement once, and also each time these policies are updated.</p>
            /// <p>To make this easy to manage, all of the 3rd party authentication flows have a `terms_agreed` field which should be set to `false` by default. If the user has agreed to the latest policies, their authentication will proceed as normal, however if their agreement is required and `terms_agreed` is set to `false` an error `403 Forbidden (error_ref --parse_errorref_NO_TERMS_AGREEMENT)` will be returned. When you receive this error, you must collect the users agreement before resubmitting the authentication flow with `terms_agreed` set to `true`, which will be recorded.</p>
            /// <p>__NOTE:__ You must make sure the Terms of Use and Privacy Policy are correctly linked, or displayed inline using the [agreements endpoints](#agreements) to get the latest versions.</p>
            /// <p>If you wish to display the agreements in a web browser overlay, we recommend adding __/widget__ and __?no_links=true__ to the end of the agreement URLs, to remove the menus and external links, for example:</p>
            /// <p>- [https://mod.io/terms`/widget?no_links=true`](https://mod.io/terms/widget?no_links=true)<br/></p>
            /// <p>- [https://mod.io/privacy`/widget?no_links=true`](https://mod.io/privacy/widget?no_links=true)</p>
            /// <p>__NOTE:__ You can use your own text and process, but be aware that you are responsible for ensuring that the users agreement is properly collected and reported. Failure to do so correctly is a breach of the [mod.io Game Terms](https://mod.io/gameterms/widget). If your game does not authenticate users or only uses the email authentication flow, you do not need to implement this dialog, but you should link to the mod.io Terms of Use and Privacy Policy in your Privacy Policy/EULA.</p>
            /// </summary>
            internal static async Task<(Error error, JToken termsObject)> TermsAsJToken(

            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/authenticate/terms", ModioAPIRequestMethod.Get, ModioAPIRequestContentType.FormUrlEncoded);


                return await _apiInterface.GetJson(request);
            }

            /// <summary>
            /// <p>The purpose of this endpoint is to provide the text, links and buttons you can use to get a users agreement and consent prior to authenticating them in-game (your dialog should look similar to the example below). This text will be localized based on the `Accept-Language` header, into one of our [supported languages](#localization) (note: our full Terms of Use and Privacy Policy are currently in English-only). If you are authenticating using platform SSO, you must call this endpoint with the `X-Modio-Portal` [header set](#targeting-a-portal), so the text is localized to match the platforms requirements. A successful response will return a [Terms Object](#terms-object).</p>
            /// <p>__Example Dialog:__</p>
            /// <p><aside class='consent'>This game uses mod.io to support user-generated content. By clicking 'I Agree' you agree to the mod.io Terms of Use and a mod.io account will be created for you (using your display name, avatar and ID). Please see the mod.io Privacy Policy on how mod.io processes your personal data.<br/><br/><div style='text-align:center'><span class='versionwrap cursor'> &amp;nbsp; &amp;nbsp; I Agree &amp;nbsp; &amp;nbsp; </span> <span class='versionwrap outline cursor'>No, Thanks</span><br/><br/>[Terms of Use](https://mod.io/terms/widget) - [Privacy Policy](https://mod.io/privacy/widget)<br/></div></aside></p>
            /// <p>__IMPORTANT:__ It is a requirement of the [Game Terms](https://mod.io/gameterms) with mod.io, and the platforms mod.io is used on, to ensure the user provides consent and has agreed to the latest mod.io [Terms of Use](https://mod.io/terms/widget) and [Privacy Policy](https://mod.io/privacy/widget). The users agreement must be collected prior to using a 3rd party authentication flow (including but not limited to Steam, PSN, Nintendo and Xbox Live). You only need to collect the users agreement once, and also each time these policies are updated.</p>
            /// <p>To make this easy to manage, all of the 3rd party authentication flows have a `terms_agreed` field which should be set to `false` by default. If the user has agreed to the latest policies, their authentication will proceed as normal, however if their agreement is required and `terms_agreed` is set to `false` an error `403 Forbidden (error_ref --parse_errorref_NO_TERMS_AGREEMENT)` will be returned. When you receive this error, you must collect the users agreement before resubmitting the authentication flow with `terms_agreed` set to `true`, which will be recorded.</p>
            /// <p>__NOTE:__ You must make sure the Terms of Use and Privacy Policy are correctly linked, or displayed inline using the [agreements endpoints](#agreements) to get the latest versions.</p>
            /// <p>If you wish to display the agreements in a web browser overlay, we recommend adding __/widget__ and __?no_links=true__ to the end of the agreement URLs, to remove the menus and external links, for example:</p>
            /// <p>- [https://mod.io/terms`/widget?no_links=true`](https://mod.io/terms/widget?no_links=true)<br/></p>
            /// <p>- [https://mod.io/privacy`/widget?no_links=true`](https://mod.io/privacy/widget?no_links=true)</p>
            /// <p>__NOTE:__ You can use your own text and process, but be aware that you are responsible for ensuring that the users agreement is properly collected and reported. Failure to do so correctly is a breach of the [mod.io Game Terms](https://mod.io/gameterms/widget). If your game does not authenticate users or only uses the email authentication flow, you do not need to implement this dialog, but you should link to the mod.io Terms of Use and Privacy Policy in your Privacy Policy/EULA.</p>
            /// </summary>
            internal static async Task<(Error error, TermsObject? termsObject)> Terms(

            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/authenticate/terms", ModioAPIRequestMethod.Get, ModioAPIRequestContentType.FormUrlEncoded);


                return await _apiInterface.GetJson<TermsObject>(request);
            }
        }
    }
}
