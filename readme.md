# ZenHubToMSTeams

Translate ZenHub webhooks to MS Teams webhooks, using AWS serverless.

## Prerequisits

- [AWS SAM CLI](https://github.com/awslabs/aws-sam-cli/releases) v0.47.0+.
- .NET Core 3.1 SDK.
- An AWS account to deploy to. **THIS WILL COUNT AGAINST YOUR AWS BILL**.
- Microsoft Teams webhook link, see [this video](https://www.youtube.com/watch?v=HhvS3Gbuaxg) on how this can be obtained.

## Installation

```
sam build
sam deploy -g
```

Then follow the steps on the screen.