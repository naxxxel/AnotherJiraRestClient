﻿using AnotherJiraRestClient.JiraModel;
using RestSharp;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Linq;
using System;

namespace AnotherJiraRestClient
{
    /// <summary>
    /// Class used for all interaction with the Jira API. See 
    /// http://docs.atlassian.com/jira/REST/latest/ for documentation of the
    /// Jira API.
    /// </summary>
    public class JiraClient
    {
        private readonly RestClient client;

        /// <summary>
        /// Constructs a JiraClient.
        /// </summary>
        /// <param name="account">Jira account information</param>
        public JiraClient(JiraAccount account)
        {
            client = new RestClient(account.ServerUrl)
            {
                Authenticator = new HttpBasicAuthenticator(account.User, account.Password)
            };
        }

        /// <summary>
        /// Executes a RestRequest and returns the deserialized response. If
        /// the response hasn't got the specified expected response code or if an
        /// exception was thrown during execution a JiraApiException will be 
        /// thrown.
        /// </summary>
        /// <typeparam name="T">Request return type</typeparam>
        /// <param name="request">request to execute</param>
        /// <param name="expectedResponseCode">The expected HTTP response code</param>
        /// <returns>deserialized response of request</returns>
        public T Execute<T>(RestRequest request, HttpStatusCode expectedResponseCode) where T : new()
        {
            // Won't throw exception.
            var response = client.Execute<T>(request);

            validateResponse(response);

            return response.Data;
        }

        /// <summary>
        /// Throws exception with details if request was not unsucessful
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        private static void validateResponse(IRestResponse response)
        {
            if (response.ResponseStatus != ResponseStatus.Completed || response.ErrorException != null || response.StatusCode == HttpStatusCode.BadRequest)
                throw new JiraApiException(string.Format("RestSharp response status: {0} - HTTP response: {1} - {2} - {3}", response.ResponseStatus, response.StatusCode, response.StatusDescription, response.Content));
        }

        /// <summary>
        /// Returns a comma separated string from the strings in the provided
        /// IEnumerable of strings. Returns an empty string if null is provided.
        /// </summary>
        /// <param name="strings">items to put in the output string</param>
        /// <returns>a comma separated string</returns>
        private static string ToCommaSeparatedString(IEnumerable<string> strings)
        {
            if (strings != null)
                return string.Join(",", strings);
            else
                return string.Empty;
        }

        /// <summary>
        /// Returns the Issue with the specified key. If the fields parameter
        /// is specified only the given field names will be loaded. Issue
        /// contains the availible field names, for example Issue.SUMMARY. Throws
        /// a JiraApiException if the request was unable to execute.
        /// </summary>
        /// <param name="issueKey">Issue key</param>
        /// <param name="fields">Fields to load</param>
        /// <returns>
        /// The issue with the specified key or null if no such issue was found.
        /// </returns>
        public Issue GetIssue(string issueKey, IEnumerable<string> fields = null)
        {
            var fieldsString = ToCommaSeparatedString(fields);

            var request = new RestRequest();
            request.Resource = string.Format("{0}?fields={1}", ResourceUrls.IssueByKey(issueKey), fieldsString);
            request.Method = Method.GET;

            var issue = Execute<Issue>(request, HttpStatusCode.OK);
            return issue.fields != null ? issue : null;
        }

        /// <summary>
        /// Searches for Issues using JQL. Throws a JiraApiException if the request 
        /// was unable to execute.
        /// </summary>
        /// <param name="jql">a JQL search string</param>
        /// <returns>The search results</returns>
        public Issues GetIssuesByJql(string jql, int startAt, int maxResults, IEnumerable<string> fields = null)
        {
            var request = new RestRequest();
            request.Resource = ResourceUrls.Search();
            request.AddParameter(new Parameter()
                {
                    Name = "jql",
                    Value = jql,
                    Type = ParameterType.GetOrPost
                });
            request.AddParameter(new Parameter()
            {
                Name = "fields",
                Value = ToCommaSeparatedString(fields),
                Type = ParameterType.GetOrPost
            });
            request.AddParameter(new Parameter()
            {
                Name = "startAt",
                Value = startAt,
                Type = ParameterType.GetOrPost
            });
            request.AddParameter(new Parameter()
            {
                Name = "maxResults",
                Value = maxResults,
                Type = ParameterType.GetOrPost
            });
            request.Method = Method.GET;
            return Execute<Issues>(request, HttpStatusCode.OK);
        }

        /// <summary>
        /// Returns the Issues for the specified project.  Throws
        /// a JiraApiException if the request was unable to execute.
        /// </summary>
        /// <param name="projectKey">project key</param>
        /// <returns>the Issues of the specified project</returns>
        public Issues GetIssuesByProject(string projectKey, int startAt, int maxResults, IEnumerable<string> fields = null)
        {
            return GetIssuesByJql("project=" + projectKey, startAt, maxResults, fields);
        }

        /// <summary>
        /// Returns all available projects the current user has permision to view. 
        /// Throws a JiraApiException if the request was unable to execute.
        /// </summary>
        /// <returns>Details of all projects visible to user</returns>
        public List<Project> GetProjects()
        {
            var request = new RestRequest()
            {
                Resource = ResourceUrls.Project(),
                RequestFormat = DataFormat.Json,
                Method = Method.GET
            };

            return Execute<List<Project>>(request, HttpStatusCode.OK);
        }

        /// <summary>
        /// Returns a list of all possible priorities.  Throws
        /// a JiraApiException if the request was unable to execute.
        /// </summary>
        /// <returns></returns>
        public List<Priority> GetPriorities()
        {
            var request = new RestRequest();
            request.Resource = ResourceUrls.Priority();
            request.Method = Method.GET;
            return Execute<List<Priority>>(request, HttpStatusCode.OK);
        }

        /// <summary>
        /// Returns the meta data for creating issues. This includes the 
        /// available projects and issue types, but not fields (fields
        /// are supported in the Jira api but not by this wrapper currently).
        /// </summary>
        /// <param name="projectKey"></param>
        /// <returns>the meta data for creating issues</returns>
        public ProjectMeta GetProjectMeta(string projectKey)
        {
            var request = new RestRequest();
            request.Resource = ResourceUrls.CreateMeta();
            request.AddParameter(new Parameter()
              {
                  Name = "projectKeys",
                  Value = projectKey,
                  Type = ParameterType.GetOrPost
              });
            request.Method = Method.GET;
            var createMeta = Execute<IssueCreateMeta>(request, HttpStatusCode.OK);
            if (createMeta.projects[0].key != projectKey || createMeta.projects.Count != 1)
                // TODO: Error message
                throw new JiraApiException();
            return createMeta.projects[0];
        }

        /// <summary>
        /// Returns a list of all possible issue statuses. Throws
        /// a JiraApiException if the request was unable to execute.
        /// </summary>
        /// <returns></returns>
        public List<Status> GetStatuses()
        {
            var request = new RestRequest();
            request.Resource = ResourceUrls.Status();
            request.Method = Method.GET;
            return Execute<List<Status>>(request, HttpStatusCode.OK);
        }

        /// <summary>
        /// Creates a new issue. Throws a JiraApiException if the request was 
        /// unable to execute.
        /// </summary>
        /// <returns>the new issue</returns>
        public BasicIssue CreateIssue(CreateIssue newIssue)
        {
            var request = new RestRequest()
            {
                Resource = ResourceUrls.Issue(),
                RequestFormat = DataFormat.Json,
                Method = Method.POST
            };

            request.AddBody(newIssue);

            return Execute<BasicIssue>(request, HttpStatusCode.Created);
        }

        /// <summary>
        /// Returns the application property with the specified key.
        /// </summary>
        /// <param name="propertyKey">the property key</param>
        /// <returns>the application property with the specified key</returns>
        public ApplicationProperty GetApplicationProperty(string propertyKey)
        {
            var request = new RestRequest()
            {
                Method = Method.GET,
                Resource = ResourceUrls.ApplicationProperties(),
                RequestFormat = DataFormat.Json
            };

            request.AddParameter(new Parameter()
            {
                Name = "key",
                Value = propertyKey,
                Type = ParameterType.GetOrPost
            });

            return Execute<ApplicationProperty>(request, HttpStatusCode.OK);
        }

        /// <summary>
        /// Returns the attachment with the specified id.
        /// </summary>
        /// <param name="attachmentId">attachment id</param>
        /// <returns>the attachment with the specified id</returns>
        public Attachment GetAttachment(string attachmentId)
        {
            var request = new RestRequest()
            {
                Method = Method.GET,
                Resource = ResourceUrls.AttachmentById(attachmentId),
                RequestFormat = DataFormat.Json
            };

            return Execute<Attachment>(request, HttpStatusCode.OK);
        }

        /// <summary>
        /// Deletes the specified attachment.
        /// </summary>
        /// <param name="attachmentId">attachment to delete</param>
        public void DeleteAttachment(string attachmentId)
        {
            var request = new RestRequest()
            {
                Method = Method.DELETE,
                Resource = ResourceUrls.AttachmentById(attachmentId)
            };

            var response = client.Execute(request);
            if (response.ResponseStatus != ResponseStatus.Completed || response.StatusCode != HttpStatusCode.NoContent)
                throw new JiraApiException("Failed to delete attachment with id=" + attachmentId);
        }

        /// <summary>
        /// Update time tracking estimates
        /// </summary>
        /// <param name="issuekey"></param>
        /// <param name="orginialEstimateMinutes"></param>
        /// <param name="remainingEstimateMinutes"></param>
        /// <returns></returns>
        public bool UpdateTimetracking(string issuekey, int? orginialEstimateMinutes, int? remainingEstimateMinutes)
        {
            dynamic trackingO = new ExpandoObject();
            if (orginialEstimateMinutes.HasValue)
                trackingO.originalEstimate = string.Format("{0}m", orginialEstimateMinutes.Value);

            if (remainingEstimateMinutes.HasValue)
                trackingO.remainingEstimate = string.Format("{0}m", remainingEstimateMinutes.Value);

            object updateObject = new
                    {
                        timetracking = new object[] {new
                        {
                            edit = trackingO
                        }}
                    };

            return PerformUpdate(issuekey, updateObject: updateObject);
        }

        /// <summary>
        /// Reset given fields to null values
        /// </summary>
        /// <param name="issuekey"></param>
        /// <param name="orginialEstimateMinutes"></param>
        /// <param name="remainingEstimateMinutes"></param>
        /// <returns></returns>
        public bool ResetFields(string issuekey, List<string> fieldNames)
        {
            dynamic dyno = new ExpandoObject();

            // Add property with "null" value
            var dynoDic = dyno as IDictionary<string, object>;
            fieldNames.ForEach(field => dynoDic.Add(field, null));

            return PerformUpdate(issuekey, fieldsObject: dyno);
        }

        public Transitions GetTransitions(string issueKey)
        {
            var request = new RestRequest()
            {
                Method = Method.GET,
                Resource = ResourceUrls.TransitionsByKey(issueKey),
                RequestFormat = DataFormat.Json
            };

            return Execute<Transitions>(request, HttpStatusCode.OK);
        }

        public bool PerformTransition(string issueKey, string transitionId)
        {
            var request = new RestRequest()
            {
                Method = Method.POST,
                Resource = ResourceUrls.TransitionsByKey(issueKey),
                RequestFormat = DataFormat.Json
            };

            request.AddBody(new
            {
                transition = new
                {
                    id = transitionId
                }
            });

            // No response expected
            var response = client.Execute(request);

            validateResponse(response);

            return response.StatusCode == HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Load only comments of an issue
        /// </summary>
        /// <param name="issueKey"></param>
        /// <returns></returns>
        public Comments GetComments(string issueKey)
        {
            var request = new RestRequest()
            {
                Method = Method.GET,
                Resource = ResourceUrls.CommentByKey(issueKey),
                RequestFormat = DataFormat.Json
            };

            return Execute<Comments>(request, HttpStatusCode.OK);
        }

        /// <summary>
        /// Currnently only supports comments visible for everyone
        /// </summary>
        /// <param name="IssueKey"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Comment AddComment(string issueKey, string message)
        {
            var request = new RestRequest()
            {
                Method = Method.POST,
                Resource = ResourceUrls.CommentByKey(issueKey),
                RequestFormat = DataFormat.Json
            };

            request.AddBody(new
            {
                body = message
            });

            return this.Execute<Comment>(request, HttpStatusCode.Created);
        }

        /// <summary>
        /// {
        //    "update": updateobject,
        //    "fields": fieldobject
        //  }
        /// </summary>
        /// <param name="issuekey"></param>
        /// <param name="bodyObject"></param>
        /// <returns></returns>
        public bool PerformUpdate(string issuekey, object fieldsObject = null, object updateObject = null)
        {
            if (fieldsObject == null && updateObject == null)
                throw new ArgumentNullException("Need at least one object");

            dynamic bodyObject = new ExpandoObject();

            if (fieldsObject != null)
                bodyObject.fields = fieldsObject;

            if (updateObject != null)
                bodyObject.update = updateObject;

            return PerformUpdate(issuekey, bodyObject);
        }

        /// <summary>
        /// {
        //    "update": updateobject,
        //    "fields": fieldobject
        //  }
        /// </summary>
        /// <param name="issuekey"></param>
        /// <param name="bodyObject"></param>
        /// <returns></returns>
        public bool PerformUpdate(string issuekey, object bodyObject)
        {
            if (bodyObject == null)
                throw new ArgumentNullException();

            var request = new RestRequest()
            {
                Resource = string.Format("{0}", ResourceUrls.IssueByKey(issuekey)),
                Method = Method.PUT,
                RequestFormat = DataFormat.Json,
            };

            request.AddBody(bodyObject);

            // No response expected
            var response = client.Execute(request);

            validateResponse(response);

            // Code 204 or 200
            return response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK;
        }

        /// <summary>
        /// Gets the version with the specified version id
        /// </summary>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public fixversion GetVersion(string versionId)
        {
            var request = new RestRequest()
            {
                Resource = string.Format("{0}", ResourceUrls.VersionById(versionId)),
                Method = Method.GET,
                RequestFormat = DataFormat.Json,
            };

            return Execute<fixversion>(request, HttpStatusCode.OK);
        }

        /// <summary>
        /// At least "name" and "project" need to be set
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public fixversion CreateVersion(fixversion version)
        {
            var request = new RestRequest()
            {
                Resource = string.Format("{0}", ResourceUrls.VersionById("")),
                Method = Method.POST,
                RequestFormat = DataFormat.Json,
            };

            request.AddBody(version);

            return Execute<fixversion>(request, HttpStatusCode.Created);
        }

        /// <summary>
        /// Deletes the version with the specified id.
        /// Issues with this fixVersion or affectedVersion will have the version removed
        /// </summary>
        /// <remarks>
        /// Could be extended in the future: /rest/api/2/version/{id}?moveFixIssuesTo&moveAffectedIssuesTo
        /// (defaults to null => remove)
        /// </remarks>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public bool DeleteVersion(string versionId)
        {
            var request = new RestRequest()
            {
                Resource = string.Format("{0}", ResourceUrls.VersionById(versionId)),
                Method = Method.DELETE,
                RequestFormat = DataFormat.Json,
            };

            // No response expected
            var response = client.Execute(request);

            validateResponse(response);

            // Code 204
            return response.StatusCode == HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Creates an issueLink
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        public bool LinkIssues(IssueLinkRequest link)
        {
            var request = new RestRequest()
            {
                Resource = string.Format("{0}", ResourceUrls.IssueLink()),
                Method = Method.POST,
                RequestFormat = DataFormat.Json,
            };

            object bodyObject = link.GetAsBodyObject(); 

            request.AddBody(bodyObject);

            // No response content expected
            var response = client.Execute(request);

            validateResponse(response);

            // Code 201 - even though API says it should be 200.
            return response.StatusCode == HttpStatusCode.Created;
        }

        public IssueLinkTypes GetIssueLinkTypes()
        {
            throw new NotImplementedException("Deserialization does not work yet. Request is valid!"); // TODO

            var request = new RestRequest()
            {
                Method = Method.GET,
                Resource = ResourceUrls.IssueLinkType(),
                RequestFormat = DataFormat.Json
            };

            return Execute<IssueLinkTypes>(request, HttpStatusCode.OK);
        }

        /// <summary>
        /// Returns information about the currently authenticated user's session. If the caller is not authenticated they will get a 401 Unauthorized status code.
        /// </summary>
        /// <returns></returns>
        public bool TestLogon()
        {
            var request = new RestRequest()
            {
                Method = Method.GET,
                Resource = ResourceUrls.Session(),
                RequestFormat = DataFormat.Json
            };

            // No response content expected
            var response = client.Execute(request);

            validateResponse(response);

            // Code 200
            return response.StatusCode == HttpStatusCode.OK;
        }
    }
}
