# AWS Deployment Guide - Job Research Agent

This guide documents the complete deployment process for the Job Research Agent on AWS using ECS Fargate + EventBridge.

## Prerequisites

- AWS CLI installed and configured
- Docker Desktop running
- .NET 8 SDK installed
- OpenAI API key

---

## Part 1: AWS CLI Setup

### 1. Install AWS CLI v2
Download and install: https://awscli.amazonaws.com/AWSCLIV2.msi

Verify installation:
```powershell
aws --version
```

### 2. Configure AWS CLI
```powershell
aws configure
```
- AWS Access Key ID: [from IAM]
- AWS Secret Access Key: [from IAM]
- Default region name: `sa-east-1`
- Default output format: `json`

---

## Part 2: S3 Storage Setup

### 3. Create S3 bucket
```powershell
aws s3api create-bucket --bucket job-research-agent-alexandregavazza-2026 --region sa-east-1 --create-bucket-configuration LocationConstraint=sa-east-1
```

### 4. Create S3 write policy file
Create `s3-write-policy.json`:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "ListBucket",
      "Effect": "Allow",
      "Action": ["s3:ListBucket"],
      "Resource": "arn:aws:s3:::job-research-agent-alexandregavazza-2026"
    },
    {
      "Sid": "WriteObjects",
      "Effect": "Allow",
      "Action": ["s3:PutObject", "s3:AbortMultipartUpload", "s3:PutObjectTagging"],
      "Resource": "arn:aws:s3:::job-research-agent-alexandregavazza-2026/*"
    }
  ]
}
```

### 5. Create IAM policy for S3 access
```powershell
aws iam create-policy --policy-name JobResearchAgentS3Write --policy-document file://s3-write-policy.json
```
Output: `arn:aws:iam::418725627679:policy/JobResearchAgentS3Write`

---

## Part 3: IAM Roles Setup

### 6. Create Lambda trust policy (kept for reference, not used in Fargate)
Create `lambda-trust-policy.json`:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "",
      "Effect": "Allow",
      "Principal": { "Service": "lambda.amazonaws.com" },
      "Action": "sts:AssumeRole"
    }
  ]
}
```

### 7. Create ECS task trust policy
Create `ecs-task-trust-policy.json`:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": { "Service": "ecs-tasks.amazonaws.com" },
      "Action": "sts:AssumeRole"
    }
  ]
}
```

### 8. Create ECS execution role
```powershell
aws iam create-role --role-name JobResearchAgentEcsExecutionRole --assume-role-policy-document file://ecs-task-trust-policy.json
```
Output: `arn:aws:iam::418725627679:role/JobResearchAgentEcsExecutionRole`

### 9. Attach execution policy to execution role
```powershell
aws iam attach-role-policy --role-name JobResearchAgentEcsExecutionRole --policy-arn arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy
```

### 10. Create ECS task role
```powershell
aws iam create-role --role-name JobResearchAgentEcsTaskRole --assume-role-policy-document file://ecs-task-trust-policy.json
```
Output: `arn:aws:iam::418725627679:role/JobResearchAgentEcsTaskRole`

### 11. Attach S3 policy to task role
```powershell
aws iam attach-role-policy --role-name JobResearchAgentEcsTaskRole --policy-arn arn:aws:iam::418725627679:policy/JobResearchAgentS3Write
```

---

## Part 4: Container Image Setup

### 12. Create ECR repository
```powershell
aws ecr create-repository --repository-name job-research-agent-fargate --image-scanning-configuration scanOnPush=true
```
Output: `418725627679.dkr.ecr.sa-east-1.amazonaws.com/job-research-agent-fargate`

### 13. Build Docker image
From workspace root:
```powershell
docker build -t job-research-agent-fargate:latest -f .\JobResearchAgent\Dockerfile .
```

**Important:** Ensure your `Dockerfile` includes:
1. **Profiles folder** with resume files:
   ```dockerfile
   COPY JobResearchAgent/Profiles ./Profiles
   ```
2. **Playwright browser installation**:
   ```dockerfile
   RUN pwsh playwright.ps1 install chromium --with-deps
   ```

Complete Dockerfile reference:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY JobResearchAgent/JobResearchAgent.csproj JobResearchAgent/
RUN dotnet restore JobResearchAgent/JobResearchAgent.csproj
COPY . .
RUN dotnet publish JobResearchAgent/JobResearchAgent.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/playwright/dotnet:v1.58.0-jammy
WORKDIR /app
ENV PLAYWRIGHT_BROWSERS_PATH=0
COPY --from=build /app/publish .
COPY JobResearchAgent/Profiles ./Profiles
RUN pwsh playwright.ps1 install chromium --with-deps
ENTRYPOINT ["dotnet", "JobResearchAgent.dll"]
```

### 14. Log in to ECR
```powershell
aws ecr get-login-password --region sa-east-1 | docker login --username AWS --password-stdin 418725627679.dkr.ecr.sa-east-1.amazonaws.com
```

### 15. Tag the image
```powershell
docker tag job-research-agent-fargate:latest 418725627679.dkr.ecr.sa-east-1.amazonaws.com/job-research-agent-fargate:latest
```

### 16. Push to ECR
```powershell
docker push 418725627679.dkr.ecr.sa-east-1.amazonaws.com/job-research-agent-fargate:latest
```

---

## Part 5: Secrets Manager Setup

### 17. Store OpenAI API key
```powershell
aws secretsmanager create-secret --name JobResearchAgent/OpenAI --secret-string "YOUR_OPENAI_API_KEY" --region sa-east-1
```
Output: `arn:aws:secretsmanager:sa-east-1:418725627679:secret:JobResearchAgent/OpenAI-h3C7zp`

### 18. Create secrets access policy
Create `secrets-policy.json`:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue"
      ],
      "Resource": "arn:aws:secretsmanager:sa-east-1:418725627679:secret:JobResearchAgent/OpenAI-h3C7zp"
    }
  ]
}
```

### 19. Create IAM policy for secrets
```powershell
aws iam create-policy --policy-name JobResearchAgentSecretsAccess --policy-document file://secrets-policy.json
```
Output: `arn:aws:iam::418725627679:policy/JobResearchAgentSecretsAccess`

### 20. Attach secrets policy to execution role
```powershell
aws iam attach-role-policy --role-name JobResearchAgentEcsExecutionRole --policy-arn arn:aws:iam::418725627679:policy/JobResearchAgentSecretsAccess
```

---

## Part 6: ECS Cluster and Task Setup

### 21. Create ECS cluster
```powershell
aws ecs create-cluster --cluster-name job-research-agent --region sa-east-1
```
Output: `arn:aws:ecs:sa-east-1:418725627679:cluster/job-research-agent`

### 22. Create CloudWatch Logs group
```powershell
aws logs create-log-group --log-group-name /ecs/job-research-agent --region sa-east-1
```

### 23. Get VPC ID
```powershell
aws ec2 describe-vpcs --filters "Name=isDefault,Values=true" --region sa-east-1 --query "Vpcs[0].VpcId" --output text
```
Output: `vpc-1bbd807c`

### 24. Get subnet IDs
```powershell
aws ec2 describe-subnets --filters "Name=vpc-id,Values=vpc-1bbd807c" --region sa-east-1 --query "Subnets[*].SubnetId" --output text
```
Output: `subnet-446fa30d subnet-5079d736 subnet-6ee05435`

### 25. Create ECS task definition file
Create `ecs-task-definition.json`:
```json
{
  "family": "job-research-agent-task",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "1024",
  "memory": "2048",
  "executionRoleArn": "arn:aws:iam::418725627679:role/JobResearchAgentEcsExecutionRole",
  "taskRoleArn": "arn:aws:iam::418725627679:role/JobResearchAgentEcsTaskRole",
  "containerDefinitions": [
    {
      "name": "job-research-agent-container",
      "image": "418725627679.dkr.ecr.sa-east-1.amazonaws.com/job-research-agent-fargate:latest",
      "essential": true,
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        }
      ],
      "secrets": [
        {
          "name": "OPENAI_API_KEY",
          "valueFrom": "arn:aws:secretsmanager:sa-east-1:418725627679:secret:JobResearchAgent/OpenAI-h3C7zp"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/job-research-agent",
          "awslogs-region": "sa-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      }
    }
  ]
}
```

### 26. Register task definition
```powershell
aws ecs register-task-definition --cli-input-json file://ecs-task-definition.json --region sa-east-1
```
Output: `arn:aws:ecs:sa-east-1:418725627679:task-definition/job-research-agent-task:1`

---

## Part 7: EventBridge Scheduling Setup

### 27. Create EventBridge trust policy
Create `eventbridge-trust-policy.json`:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": { "Service": "events.amazonaws.com" },
      "Action": "sts:AssumeRole"
    }
  ]
}
```

### 28. Create EventBridge execution role
```powershell
aws iam create-role --role-name JobResearchAgentEventBridgeRole --assume-role-policy-document file://eventbridge-trust-policy.json
```
Output: `arn:aws:iam::418725627679:role/JobResearchAgentEventBridgeRole`

### 29. Create EventBridge ECS policy
Create `eventbridge-ecs-policy.json`:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "ecs:RunTask"
      ],
      "Resource": "arn:aws:ecs:sa-east-1:418725627679:task-definition/job-research-agent-task:*"
    },
    {
      "Effect": "Allow",
      "Action": "iam:PassRole",
      "Resource": [
        "arn:aws:iam::418725627679:role/JobResearchAgentEcsExecutionRole",
        "arn:aws:iam::418725627679:role/JobResearchAgentEcsTaskRole"
      ]
    }
  ]
}
```

### 30. Create IAM policy for EventBridge
```powershell
aws iam create-policy --policy-name JobResearchAgentEventBridgeEcsPolicy --policy-document file://eventbridge-ecs-policy.json
```
Output: `arn:aws:iam::418725627679:policy/JobResearchAgentEventBridgeEcsPolicy`

### 31. Attach policy to EventBridge role
```powershell
aws iam attach-role-policy --role-name JobResearchAgentEventBridgeRole --policy-arn arn:aws:iam::418725627679:policy/JobResearchAgentEventBridgeEcsPolicy
```

### 32. Create morning schedule (8am Brazil / 11am UTC)
```powershell
aws events put-rule --name job-research-agent-morning --schedule-expression "cron(0 11 * * ? *)" --state ENABLED --region sa-east-1
```
Output: `arn:aws:events:sa-east-1:418725627679:rule/job-research-agent-morning`

### 33. Create morning target configuration
Create `morning-target.json`:
```json
[
  {
    "Id": "1",
    "Arn": "arn:aws:ecs:sa-east-1:418725627679:cluster/job-research-agent",
    "RoleArn": "arn:aws:iam::418725627679:role/JobResearchAgentEventBridgeRole",
    "EcsParameters": {
      "TaskDefinitionArn": "arn:aws:ecs:sa-east-1:418725627679:task-definition/job-research-agent-task:1",
      "TaskCount": 1,
      "LaunchType": "FARGATE",
      "NetworkConfiguration": {
        "awsvpcConfiguration": {
          "Subnets": ["subnet-446fa30d", "subnet-5079d736", "subnet-6ee05435"],
          "AssignPublicIp": "ENABLED"
        }
      }
    }
  }
]
```

### 34. Attach morning target
```powershell
aws events put-targets --rule job-research-agent-morning --targets file://morning-target.json --region sa-east-1
```

### 35. Create evening schedule (5pm Brazil / 8pm UTC)
```powershell
aws events put-rule --name job-research-agent-evening --schedule-expression "cron(0 20 * * ? *)" --state ENABLED --region sa-east-1
```
Output: `arn:aws:events:sa-east-1:418725627679:rule/job-research-agent-evening`

### 36. Create evening target configuration
Create `evening-target.json`:
```json
[
  {
    "Id": "1",
    "Arn": "arn:aws:ecs:sa-east-1:418725627679:cluster/job-research-agent",
    "RoleArn": "arn:aws:iam::418725627679:role/JobResearchAgentEventBridgeRole",
    "EcsParameters": {
      "TaskDefinitionArn": "arn:aws:ecs:sa-east-1:418725627679:task-definition/job-research-agent-task:1",
      "TaskCount": 1,
      "LaunchType": "FARGATE",
      "NetworkConfiguration": {
        "awsvpcConfiguration": {
          "Subnets": ["subnet-446fa30d", "subnet-5079d736", "subnet-6ee05435"],
          "AssignPublicIp": "ENABLED"
        }
      }
    }
  }
]
```

### 37. Attach evening target
```powershell
aws events put-targets --rule job-research-agent-evening --targets file://evening-target.json --region sa-east-1
```

---

## Part 8: RDS PostgreSQL Database Setup

### 38. Create RDS security group
```powershell
aws ec2 create-security-group --group-name job-research-agent-rds-sg --description "Security group for Job Research Agent RDS" --vpc-id vpc-1bbd807c --region sa-east-1
```
Output: `sg-057ec7832807d06ea`

### 39. Allow PostgreSQL access from VPC
```powershell
aws ec2 authorize-security-group-ingress --group-id sg-057ec7832807d06ea --protocol tcp --port 5432 --cidr 172.31.0.0/16 --region sa-east-1
```

### 40. Create RDS PostgreSQL instance
```powershell
aws rds create-db-instance --db-instance-identifier job-research-agent-db --db-instance-class db.t3.micro --engine postgres --engine-version 16.12 --master-username postgres --master-user-password "YOUR_MASTER_PASSWORD" --allocated-storage 20 --vpc-security-group-ids sg-057ec7832807d06ea --db-name jobsdb --backup-retention-period 7 --region sa-east-1 --publicly-accessible --no-multi-az
```

### 41. Wait for RDS to become available
Check status:
```powershell
aws rds describe-db-instances --db-instance-identifier job-research-agent-db --region sa-east-1 --query "DBInstances[0].[DBInstanceStatus,Endpoint.Address]" --output text
```
Output: `job-research-agent-db.chsiiyek8wor.sa-east-1.rds.amazonaws.com`

### 42. Store database connection string in Secrets Manager
```powershell
aws secretsmanager create-secret --name JobResearchAgent/DatabaseConnection --secret-string "Host=job-research-agent-db.chsiiyek8wor.sa-east-1.rds.amazonaws.com;Port=5432;Database=jobsdb;Username=postgres;Password=YOUR_MASTER_PASSWORD" --region sa-east-1
```
Output: `arn:aws:secretsmanager:sa-east-1:418725627679:secret:JobResearchAgent/DatabaseConnection-24Di9R`

### 43. Update secrets policy to include database access
Update `secrets-policy.json`:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue"
      ],
      "Resource": [
        "arn:aws:secretsmanager:sa-east-1:418725627679:secret:JobResearchAgent/OpenAI-h3C7zp",
        "arn:aws:secretsmanager:sa-east-1:418725627679:secret:JobResearchAgent/DatabaseConnection-24Di9R"
      ]
    }
  ]
}
```

### 44. Update IAM secrets policy
```powershell
aws iam create-policy-version --policy-arn arn:aws:iam::418725627679:policy/JobResearchAgentSecretsAccess --policy-document file://secrets-policy.json --set-as-default
```

### 45. Update ECS task definition with database connection
Update `ecs-task-definition.json` to include database secret:
```json
"secrets": [
  {
    "name": "OPENAI_API_KEY",
    "valueFrom": "arn:aws:secretsmanager:sa-east-1:418725627679:secret:JobResearchAgent/OpenAI-h3C7zp"
  },
  {
    "name": "ConnectionStrings__Default",
    "valueFrom": "arn:aws:secretsmanager:sa-east-1:418725627679:secret:JobResearchAgent/DatabaseConnection-24Di9R"
  }
]
```

### 46. Register updated task definition
```powershell
aws ecs register-task-definition --cli-input-json file://ecs-task-definition.json --region sa-east-1
```
Output: `job-research-agent-task:3`

### 47. Update EventBridge targets with new revision
Update both `morning-target.json` and `evening-target.json` to use revision 3:
```json
"TaskDefinitionArn": "arn:aws:ecs:sa-east-1:418725627679:task-definition/job-research-agent-task:3"
```

Then run:
```powershell
aws events put-targets --rule job-research-agent-morning --targets file://morning-target.json --region sa-east-1
aws events put-targets --rule job-research-agent-evening --targets file://evening-target.json --region sa-east-1
```

---

## Part 9: Docker Build and Push (Final)

### 48. Configure application settings securely

**For Production (appsettings.json - tracked in git):**
Use placeholder values for sensitive data:
```json
"ConnectionStrings": {
  "Default": "Host=job-research-agent-db.chsiiyek8wor.sa-east-1.rds.amazonaws.com;Port=5432;Database=jobsdb;Username=postgres;Password=YOUR_PASSWORD_HERE"
},
"Storage": {
  "S3Bucket": "",
  "S3Prefix": "documents"
}
```

**For Local Development (appsettings.Development.json - in .gitignore):**
Use real credentials (this file is NOT committed to git):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ConnectionStrings": {
    "Default": "Host=job-research-agent-db.chsiiyek8wor.sa-east-1.rds.amazonaws.com;Port=5432;Database=jobsdb;Username=postgres;Password=YOUR_ACTUAL_PASSWORD"
  }
}
```

**Note:** Production credentials come from AWS Secrets Manager via ECS task definition environment variables.

### 49. Rebuild Docker image
From workspace root:
```powershell
docker build -t job-research-agent-fargate:latest -f .\JobResearchAgent\Dockerfile .
```

### 50. Tag image for ECR
```powershell
docker tag job-research-agent-fargate:latest 418725627679.dkr.ecr.sa-east-1.amazonaws.com/job-research-agent-fargate:latest
```

### 51. Push image to ECR
```powershell
docker push 418725627679.dkr.ecr.sa-east-1.amazonaws.com/job-research-agent-fargate:latest
```

### 52. Update ECS task definition with S3 configuration
Update `ecs-task-definition.json` to include S3 environment variables:
```json
"environment": [
  {
    "name": "ASPNETCORE_ENVIRONMENT",
    "value": "Production"
  },
  {
    "name": "Storage__S3Bucket",
    "value": "job-research-agent-alexandregavazza-2026"
  },
  {
    "name": "Storage__S3Prefix",
    "value": "documents"
  }
]
```

### 53. Register updated task definition
```powershell
aws ecs register-task-definition --cli-input-json file://ecs-task-definition.json --region sa-east-1
```
Output: `job-research-agent-task:3`

### 54. Update EventBridge targets with new revision
Update both `morning-target.json` and `evening-target.json`:
```json
"TaskDefinitionArn": "arn:aws:ecs:sa-east-1:418725627679:task-definition/job-research-agent-task:3"
```

Then update both rules:
```powershell
aws events put-targets --rule job-research-agent-morning --targets file://morning-target.json --region sa-east-1
aws events put-targets --rule job-research-agent-evening --targets file://evening-target.json --region sa-east-1
```

---

## Testing the Deployment

### Manual Test Run
Trigger a task immediately:
```powershell
aws ecs run-task --cluster job-research-agent --task-definition job-research-agent-task:3 --launch-type FARGATE --network-configuration "awsvpcConfiguration={subnets=[subnet-446fa30d,subnet-5079d736,subnet-6ee05435],assignPublicIp=ENABLED}" --region sa-east-1
```

### Monitor Logs
```powershell
aws logs tail /ecs/job-research-agent --follow --region sa-east-1
```

### Check S3 Documents
```powershell
aws s3 ls s3://job-research-agent-alexandregavazza-2026/documents/ --recursive
```

### Verify Database
```powershell
$env:PGPASSWORD="YOUR_MASTER_PASSWORD"
psql -h job-research-agent-db.chsiiyek8wor.sa-east-1.rds.amazonaws.com -U postgres -d jobsdb -c "SELECT COUNT(*) FROM jobs;"
```

---

### Monitor CloudWatch Logs
```powershell
aws logs tail /ecs/job-research-agent --follow --region sa-east-1
```

### Check S3 uploaded documents
```powershell
aws s3 ls s3://job-research-agent-alexandregavazza-2026/documents/ --recursive
```

### Manually trigger test run
```powershell
# Get valid subnet ID first
$SUBNET=$(aws ec2 describe-subnets --region sa-east-1 --filters "Name=default-for-az,Values=true" --query "Subnets[0].SubnetId" --output text)

# Run task
aws ecs run-task --cluster job-research-agent --task-definition job-research-agent-task:3 --launch-type FARGATE --network-configuration "awsvpcConfiguration={subnets=[$SUBNET],securityGroups=[sg-057ec7832807d06ea],assignPublicIp=ENABLED}" --region sa-east-1
```

---

## Troubleshooting Common Issues

### Issue: "Human resume file not found"
**Cause:** Docker image doesn't include the Profiles folder.

**Solution:** 
Ensure your Dockerfile has:
```dockerfile
COPY JobResearchAgent/Profiles ./Profiles
```
Rebuild and push the image (see step 13).

---

### Issue: "Playwright was just installed or updated"
**Cause:** Playwright browsers aren't installed in the Docker container.

**Solution:**
Add to Dockerfile after copying files:
```dockerfile
RUN pwsh playwright.ps1 install chromium --with-deps
```
Rebuild and push the image.

---

### Issue: Task fails to start / subnet not found
**Cause:** Invalid subnet ID in run-task command.

**Solution:**
Get valid subnet dynamically:
```powershell
$SUBNET=$(aws ec2 describe-subnets --region sa-east-1 --filters "Name=default-for-az,Values=true" --query "Subnets[0].SubnetId" --output text)
```

---

### Issue: Can't connect to RDS from local machine
**Cause:** Your IP is not in the security group.

**Solution:**
```powershell
$MY_IP=$(Invoke-WebRequest -Uri 'https://checkip.amazonaws.com' -UseBasicParsing).Content.Trim()
aws ec2 authorize-security-group-ingress --group-id sg-057ec7832807d06ea --protocol tcp --port 5432 --cidr "$MY_IP/32" --region sa-east-1 --description "My local IP"
```

---

### Issue: Data export/import errors (JSON/timestamp issues)
**Cause:** Using `--inserts` format doesn't properly escape JSON and complex data types.

**Solution:**
Use PostgreSQL's custom binary format:
```powershell
# Export
pg_dump -U postgres -d jobsdb --data-only --format=custom --file=export_data.dump -t jobs -t job_tailored_resumes -t job_application_logs

# Import
$env:PGPASSWORD="YOUR_PASSWORD"
pg_restore -h RDS_ENDPOINT -U postgres -d jobsdb -v export_data.dump
```

---

### Monitoring Task Execution

**View logs in real-time:**
```powershell
aws logs tail /ecs/job-research-agent --follow --region sa-east-1
```

**Check task status:**
```powershell
aws ecs describe-tasks --cluster job-research-agent --tasks TASK_ID --region sa-east-1 --query "tasks[0].{Status:lastStatus,StoppedReason:stoppedReason}"
```

**Query database for new records:**
```sql
SELECT COUNT(*) FROM job_application_logs WHERE created_at >= CURRENT_DATE;
```

---

## Summary of Resources Created

- **S3 Bucket**: `job-research-agent-alexandregavazza-2026`
- **ECR Repository**: `job-research-agent-fargate`
- **ECS Cluster**: `job-research-agent`
- **ECS Task Definition**: `job-research-agent-task:3`
- **RDS PostgreSQL**: `job-research-agent-db` (PostgreSQL 16.12, db.t3.micro, 20GB)
- **Security Groups**:
  - `job-research-agent-rds-sg` (PostgreSQL access)
- **IAM Roles**:
  - `JobResearchAgentEcsExecutionRole`
  - `JobResearchAgentEcsTaskRole`
  - `JobResearchAgentEventBridgeRole`
- **IAM Policies**:
  - `JobResearchAgentS3Write`
  - `JobResearchAgentSecretsAccess`
  - `JobResearchAgentEventBridgeEcsPolicy`
- **Secrets Manager**:
  - `JobResearchAgent/OpenAI`
  - `JobResearchAgent/DatabaseConnection`
- **EventBridge Rules**:
  - `job-research-agent-morning` (8am Brazil)
  - `job-research-agent-evening` (5pm Brazil)
- **CloudWatch Log Group**: `/ecs/job-research-agent`

---

## Updating the Application

When you make code changes:

1. Rebuild the Docker image:
```powershell
docker build -t job-research-agent-fargate:latest -f .\JobResearchAgent\Dockerfile .
```

2. Tag and push:
```powershell
docker tag job-research-agent-fargate:latest 418725627679.dkr.ecr.sa-east-1.amazonaws.com/job-research-agent-fargate:latest
docker push 418725627679.dkr.ecr.sa-east-1.amazonaws.com/job-research-agent-fargate:latest
```

3. Register new task definition revision (rerun step 26)

4. Update EventBridge targets with new task definition revision

---RDS PostgreSQL**: db.t3.micro (~$15/month) + 20GB storage (~$2/month)
- **S3**: Storage + requests
- **ECR**: Storage for container images
- **CloudWatch Logs**: Log storage and ingestion
- **Secrets Manager**: $0.40/secret/month + API calls (2 secrets = $0.80/month)
- **EventBridge**: Free for scheduled rules
- **Data Transfer**: Egress charges may apply

**Estimated cost for 2 runs/day**: ~$25-35estion
- **Secrets Manager**: $0.40/secret/month + API calls
- **EventBridge**: Free for scheduled rules
- **Data Transfer**: Egress charges may apply

**Estimated cost for 2 runs/day**: ~$10-20/month (excluding OpenAI API usage)
