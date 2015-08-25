using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnotherJiraRestClient
{
    public class IssueLinkRequest
    {
        // JSON: type:name
        public string TypeName { get; set; }
        // JSON: inwardIssue:key
        public string InwardIssueKey { get; set; }
        // JSON: outwardIssue:key
        public string OutwardIssueKey { get; set; }
        public Comment Comment { get; set; }

        public object GetAsBodyObject()
        {
            return new
            {
                type = new
                {
                    name = TypeName
                },
                inwardIssue = new
                {
                    key = InwardIssueKey
                },
                outwardIssue = new
                {
                    key = OutwardIssueKey
                },
                comment = Comment
            };
        }
    }
}
