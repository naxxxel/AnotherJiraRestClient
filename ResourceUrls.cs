using System.IO;

namespace AnotherJiraRestClient
{
    public static class ResourceUrls
    {
        private const string BaseUrl = "/rest/api/2/";

        public static string IssueByKey(string issueKey)
        {
            return Url(string.Format("issue/{0}", issueKey));
        }

        public static string Issue()
        {
            return Url("issue");
        }

        public static string Search()
        {
            return Url("search");
        }

        public static string Priority()
        {
            return Url("priority");
        }

        public static string CreateMeta()
        {
            return Url("issue/createmeta");
        }

        public static string Status()
        {
            return Url("status");
        }

        public static string ApplicationProperties()
        {
            return Url("application-properties");
        }

        public static string AttachmentById(string attachmentId)
        {
            return Url(string.Format("attachment/{0}", attachmentId));
        }

        public static string Project()
        {
            return Url("project");
        }

        public static string TransitionsByKey(string issueKey)
        {
            return string.Format("{0}/transitions", ResourceUrls.IssueByKey(issueKey));
        }

        public static string CommentByKey(string issueKey)
        {
            return string.Format("{0}/comment", ResourceUrls.IssueByKey(issueKey));
        }

        public static string VersionById(string versionId)
        {
            return Url(string.Format("version/{0}", versionId));
        }

        public static string IssueLink()
        {
            return Url("issueLink");
        }

        public static string IssueLinkType()
        {
            return Url("issueLinkType");
        }

        public static string Session()
        {
            return "/rest/auth/1/session";
        }

        private static string Url(string key)
        {
            return Path.Combine(BaseUrl, key);
        }
    }
}