#!/bin/bash

# Run production container with all required environment variables
docker run -d --name gcc-production \
  -p 5105:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/gcc_database.db" \
  -e DataProtection__PersistKeysToAzureBlobStorage=false \
  -e AzureAdB2C__ClientSecret="dummy-secret-for-testing" \
  -e AzureAd__ClientSecret="dummy-secret-for-testing" \
  -e AzureStorageBlob__ConnectionString="UseDevelopmentStorage=true" \
  -e ApplicationInsights__ConnectionString="InstrumentationKey=00000000-0000-0000-0000-000000000000" \
  -e EmailConfiguration__Password="dummy-password" \
  -e GCC_AZURE_AD_B2C_CLIENT_SECRET="dummy-secret" \
  -e GCC_AZURE_AD_CLIENT_SECRET="dummy-secret" \
  -e GCC_AZURE_BLOB_CONNECTION_STRING="UseDevelopmentStorage=true" \
  -e GCC_APPLICATION_INSIGHTS_CONNECTION_STRING="InstrumentationKey=00000000-0000-0000-0000-000000000000" \
  -e GCC_SMTP_PASSWORD="dummy-password" \
  -e AZURE_AD_B2C_CLIENT_SECRET="dummy-secret" \
  -e AZURE_AD_CLIENT_SECRET="dummy-secret" \
  -e AZURE_BLOB_CONNECTION_STRING="UseDevelopmentStorage=true" \
  -e APPLICATION_INSIGHTS_CONNECTION_STRING="InstrumentationKey=00000000-0000-0000-0000-000000000000" \
  -e SMTP_PASSWORD="dummy-password" \
  gcc-webapp:production

echo "Container started. Checking status..."
sleep 10
curl -I http://localhost:5105 && echo "Success!" || echo "Failed - check logs with: docker logs gcc-production"