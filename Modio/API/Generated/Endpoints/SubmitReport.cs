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
        public static partial class Reports
        {
            /// <summary>
            /// <p>The purpose of this endpoint is enable users to report a resource (game, mod or user) on mod.io. Successful request will return [Message Object](#message-object).</p>
            /// <p>__IMPORTANT:__ It is a requirement of the [Game Terms](https://mod.io/gameterms) with mod.io, and the platforms mod.io is used on (including but not limited to Steam, PlayStation, Nintendo and Xbox), to ensure all displayed content is reportable by users. You can enable a resource to be reported (by implementing a dialog similar to the example below) or linking to the report page on mod.io.</p>
            /// <p>__Example Dialog:__</p>
            /// <p><aside class='consent'>Report content that is not working, or violates the mod.io Terms of Use using the form below. If you’d like to report Copyright Infringement and are the Copyright holder, select "<a href='#' class='toggledmca'>DMCA</a>" as the report type.<br/><br/>Be aware all details provided will be shared with mod.io staff and the games moderators for review, and maybe shared with the content creator.<br/><br/><label>__Reason for reporting*__<br/><select class='checkdmca' required><option value='' selected></option><option value='1'>DMCA</option><option value='2'>Not working</option><option value='3'>Rude content</option><option value='4'>Illegal content</option><option value='5'>Stolen content</option><option value='6'>False information</option><option value='7'>Other</option></select></label><br/><br/><label>__Email__<br/>Optional, if you wish to be contacted if necessary regarding your report.<br/><input type='email' class='text'></label><br/><br/><label class='dmca' style='display: none'>__Company or name whose IP is being infringed__<br/><input type='text' class='text'><br/><br/></label><label>__Details of your report*__<br/>To help process your report, include all relevant information and links.<br/><textarea cols='80' rows='3' style='width: 100%' required></textarea></label><br/><br/><label class='dmca checkbox' style='display: none'><input type='checkbox'> I certify that I am the copyright owner or I am authorized to act on the copyright owner's behalf in this situation.<br/><br/></label><label class='dmca checkbox' style='display: none'><input type='checkbox'> I certify that all material in this claim is correct and not authorized by the copyright owner, its agent, or the law.<br/><br/></label><div style='text-align:center'><span class='versionwrap cursor'> &amp;nbsp; &amp;nbsp; Submit Report &amp;nbsp; &amp;nbsp; </span> <span class='versionwrap outline cursor'>Cancel</span><br/><br/>[Terms of Use](https://mod.io/terms/widget) - [Privacy Policy](https://mod.io/privacy/widget)<br/></div></aside></p>
            /// <p>__NOTE:__ If implementing your own report dialog, the Terms of Use and Privacy Policy must be correctly linked, or displayed inline using the [agreements endpoints](#agreements) to get the latest versions.</p>
            /// <p>If you wish to display the agreements in a web browser overlay, we recommend adding __/widget__ and __?no_links=true__ to the end of the agreement URLs, to remove the menus and external links, for example:</p>
            /// <p>- [https://mod.io/terms`/widget?no_links=true`](https://mod.io/terms/widget?no_links=true)<br/></p>
            /// <p>- [https://mod.io/privacy`/widget?no_links=true`](https://mod.io/privacy/widget?no_links=true)</p>
            /// <p>__NOTE:__ If you prefer to display the report page in a web browser overlay, and you know the resource you want to report, the best URL to use is: __https://mod.io/report/`resource`/`id`/widget__</p>
            /// <p>For example to report a mod with an ID of 1 the URL would be: [https://mod.io/report/`mods`/`1`/widget](https://mod.io/report/mods/1/widget). If you don't know the ID of the resource you wish to report, you can use the generic report URL: [https://mod.io/report/widget](https://mod.io/report)</p>
            /// <p>__NOTE:__ If you are a game owner or manager, you can [view all reports](https://mod.io/content) submitted for your game(s), and you are responsible for actioning reports. You can also configure in your game's control panel the number of reports required before content is automatically taken down for review.</p>
            /// <p>Read our [Terms of Use](https://mod.io/terms/widget) for information about what is/isn't acceptable.</p>
            /// </summary>
            internal static async Task<(Error error, JToken addReportResponse)> SubmitReportAsJToken(
                AddReportRequest? body = null
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/report", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.AddBody(body);
                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson(request);
            }

            /// <summary>
            /// <p>The purpose of this endpoint is enable users to report a resource (game, mod or user) on mod.io. Successful request will return [Message Object](#message-object).</p>
            /// <p>__IMPORTANT:__ It is a requirement of the [Game Terms](https://mod.io/gameterms) with mod.io, and the platforms mod.io is used on (including but not limited to Steam, PlayStation, Nintendo and Xbox), to ensure all displayed content is reportable by users. You can enable a resource to be reported (by implementing a dialog similar to the example below) or linking to the report page on mod.io.</p>
            /// <p>__Example Dialog:__</p>
            /// <p><aside class='consent'>Report content that is not working, or violates the mod.io Terms of Use using the form below. If you’d like to report Copyright Infringement and are the Copyright holder, select "<a href='#' class='toggledmca'>DMCA</a>" as the report type.<br/><br/>Be aware all details provided will be shared with mod.io staff and the games moderators for review, and maybe shared with the content creator.<br/><br/><label>__Reason for reporting*__<br/><select class='checkdmca' required><option value='' selected></option><option value='1'>DMCA</option><option value='2'>Not working</option><option value='3'>Rude content</option><option value='4'>Illegal content</option><option value='5'>Stolen content</option><option value='6'>False information</option><option value='7'>Other</option></select></label><br/><br/><label>__Email__<br/>Optional, if you wish to be contacted if necessary regarding your report.<br/><input type='email' class='text'></label><br/><br/><label class='dmca' style='display: none'>__Company or name whose IP is being infringed__<br/><input type='text' class='text'><br/><br/></label><label>__Details of your report*__<br/>To help process your report, include all relevant information and links.<br/><textarea cols='80' rows='3' style='width: 100%' required></textarea></label><br/><br/><label class='dmca checkbox' style='display: none'><input type='checkbox'> I certify that I am the copyright owner or I am authorized to act on the copyright owner's behalf in this situation.<br/><br/></label><label class='dmca checkbox' style='display: none'><input type='checkbox'> I certify that all material in this claim is correct and not authorized by the copyright owner, its agent, or the law.<br/><br/></label><div style='text-align:center'><span class='versionwrap cursor'> &amp;nbsp; &amp;nbsp; Submit Report &amp;nbsp; &amp;nbsp; </span> <span class='versionwrap outline cursor'>Cancel</span><br/><br/>[Terms of Use](https://mod.io/terms/widget) - [Privacy Policy](https://mod.io/privacy/widget)<br/></div></aside></p>
            /// <p>__NOTE:__ If implementing your own report dialog, the Terms of Use and Privacy Policy must be correctly linked, or displayed inline using the [agreements endpoints](#agreements) to get the latest versions.</p>
            /// <p>If you wish to display the agreements in a web browser overlay, we recommend adding __/widget__ and __?no_links=true__ to the end of the agreement URLs, to remove the menus and external links, for example:</p>
            /// <p>- [https://mod.io/terms`/widget?no_links=true`](https://mod.io/terms/widget?no_links=true)<br/></p>
            /// <p>- [https://mod.io/privacy`/widget?no_links=true`](https://mod.io/privacy/widget?no_links=true)</p>
            /// <p>__NOTE:__ If you prefer to display the report page in a web browser overlay, and you know the resource you want to report, the best URL to use is: __https://mod.io/report/`resource`/`id`/widget__</p>
            /// <p>For example to report a mod with an ID of 1 the URL would be: [https://mod.io/report/`mods`/`1`/widget](https://mod.io/report/mods/1/widget). If you don't know the ID of the resource you wish to report, you can use the generic report URL: [https://mod.io/report/widget](https://mod.io/report)</p>
            /// <p>__NOTE:__ If you are a game owner or manager, you can [view all reports](https://mod.io/content) submitted for your game(s), and you are responsible for actioning reports. You can also configure in your game's control panel the number of reports required before content is automatically taken down for review.</p>
            /// <p>Read our [Terms of Use](https://mod.io/terms/widget) for information about what is/isn't acceptable.</p>
            /// </summary>
            /// <param name="body"></param>
            internal static async Task<(Error error, AddReportResponse? addReportResponse)> SubmitReport(
                AddReportRequest? body = null
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/report", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.AddBody(body);
                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson<AddReportResponse>(request);
            }
        }
    }
}
