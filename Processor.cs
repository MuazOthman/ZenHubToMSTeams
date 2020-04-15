using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Net.Http;
using System;
using System.Text;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace WebhookProcessing
{

    public class Processor
    {

        private readonly HttpClient httpClient;
        private readonly string teamsWebhook;
        public Processor()
        {
            httpClient = new HttpClient();
            teamsWebhook = Environment.GetEnvironmentVariable("TEAMS_WEBHOOK");
        }
        public async Task<APIGatewayProxyResponse> Invoke(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            context.Logger.LogLine(JsonConvert.SerializeObject(apigProxyEvent));

            // documentation for ZenHub webhooks: https://github.com/ZenHubIO/API#custom-webhooks

            var nvc = HttpUtility.ParseQueryString(apigProxyEvent.Body);
            var parameters = nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
            context.Logger.LogLine(JsonConvert.SerializeObject(parameters));
            object teamsWebhookObject = null;
            if (parameters.ContainsKey("type"))
            {
                switch (parameters["type"])
                {
                    case "issue_transfer":
                        teamsWebhookObject = new TeamsPayload
                        {
                            Summary = "Issue moved",
                            Sections = new List<Section>{
                                new Section
                                {
                                    ActivityTitle= $"{parameters["user_name"]} moved issue #{parameters["issue_number"]} {parameters["issue_title"]}",
                                    Facts = new List<Fact>{
                                        new Fact
                                        {
                                            Name= "From Pipeline:",
                                            Value= parameters["from_pipeline_name"]
                                        },
                                        new Fact
                                        {
                                            Name= "To Pipeline:",
                                            Value= parameters["to_pipeline_name"]
                                        },
                                        new Fact
                                        {
                                            Name= "Repository:",
                                            Value= $"{parameters["organization"] }/{parameters["repo"]}"
                                        }
                                    }
                                }
                            },
                            PotentialAction = new List<PotentialAction>{
                                new PotentialAction
                                {
                                    Type = "OpenUri",
                                    Name = "Open in GitHub",
                                    Targets = new List<Target>{
                                        new Target{
                                            Os = "default",
                                            Uri = parameters["github_url"]
                                        }

                                    }
                                },
                                new PotentialAction
                                {
                                    Type = "OpenUri",
                                    Name = "View ZenHub Board",
                                    Targets = new List<Target>{
                                        new Target{
                                            Os = "default",
                                            Uri = $"https://github.com/{parameters["organization"] }/{parameters["repo"]}/#workspaces/{parameters["workspace_id"]}/boards"
                                        }

                                    }
                                }
                            }
                        };
                        break;
                }
            }

            if (teamsWebhookObject != null)
            {
                var payload = (JsonConvert.SerializeObject(teamsWebhookObject));
                context.Logger.LogLine($"Sending the following payload to teams webhook {teamsWebhook}");
                context.Logger.LogLine(payload);
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(teamsWebhook, httpContent);
            }

            return new APIGatewayProxyResponse
            {
                Body = apigProxyEvent.Body,
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }

    // for the actual schema see https://docs.microsoft.com/en-us/outlook/actionable-messages/message-card-reference
    public partial class TeamsPayload
    {
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("sections")]
        public List<Section> Sections { get; set; }

        [JsonProperty("potentialAction")]
        public List<PotentialAction> PotentialAction { get; set; }
    }

    public partial class PotentialAction
    {
        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("targets")]
        public List<Target> Targets { get; set; }
    }

    public partial class Target
    {
        [JsonProperty("os")]
        public string Os { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; }
    }

    public partial class Section
    {
        [JsonProperty("activityTitle")]
        public string ActivityTitle { get; set; }

        [JsonProperty("facts")]
        public List<Fact> Facts { get; set; }
    }

    public partial class Fact
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
