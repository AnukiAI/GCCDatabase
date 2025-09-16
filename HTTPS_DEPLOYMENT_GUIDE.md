# HTTPS Docker Deployment Guide

This guide explains how to deploy the GCC Web Application with HTTPS support in Docker containers.

## Quick Start

### Option 1: Simple HTTPS Deployment (Recommended for Testing)
```bash
# Windows
./docker-run-https.cmd

# Linux/Mac
docker build -f Dockerfile.https -t gcc-webapp:https .
docker run -d \
  --name gcc-webapp-https \
  -p 8443:8081 \
  -p 8080:8080 \
  -v ./certs:/https:ro \
  -v ./data:/app/data \
  gcc-webapp:https
```

Access at: https://localhost:8443

### Option 2: Docker Compose with Full Stack
```bash
# Start all services with HTTPS
docker-compose -f docker-compose.https.yml up -d

# View logs
docker-compose -f docker-compose.https.yml logs -f

# Stop all services
docker-compose -f docker-compose.https.yml down
```

## Certificate Options

### 1. Development Certificate (Auto-Generated)
The default configuration uses a self-signed development certificate that's automatically generated. This will show a browser warning but is fine for testing.

```bash
# Certificate is auto-generated in Dockerfile.https
# Password: SecurePassword123!
# Location: /https/aspnetapp.pfx
```

### 2. Your Own Certificate (PFX Format)
```bash
# Place your certificate in the certs directory
cp your-certificate.pfx ./certs/

# Update environment variables
ASPNETCORE_Kestrel__Certificates__Default__Path=/https/your-certificate.pfx
ASPNETCORE_Kestrel__Certificates__Default__Password=YourCertPassword
```

### 3. Let's Encrypt Certificate (Production)
```bash
# Use certbot to generate certificates
certbot certonly --standalone -d your-domain.com

# Mount the certificates
docker run -d \
  --name gcc-webapp-https \
  -v /etc/letsencrypt/live/your-domain.com/fullchain.pem:/https/fullchain.pem:ro \
  -v /etc/letsencrypt/live/your-domain.com/privkey.pem:/https/privkey.pem:ro \
  -e ASPNETCORE_Kestrel__Endpoints__Https__Certificate__Path=/https/fullchain.pem \
  -e ASPNETCORE_Kestrel__Endpoints__Https__Certificate__KeyPath=/https/privkey.pem \
  gcc-webapp:https
```

### 4. Using Nginx Reverse Proxy (Best for Production)
The docker-compose.https.yml includes an optional Nginx configuration:

```bash
# Start with nginx profile
docker-compose -f docker-compose.https.yml --profile production up -d
```

Create `nginx.conf`:
```nginx
events {
    worker_connections 1024;
}

http {
    upstream webapp {
        server gccwebapp:8081;
    }

    server {
        listen 80;
        server_name your-domain.com;
        return 301 https://$server_name$request_uri;
    }

    server {
        listen 443 ssl;
        server_name your-domain.com;

        ssl_certificate /etc/nginx/certs/fullchain.pem;
        ssl_certificate_key /etc/nginx/certs/privkey.pem;

        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;

        location / {
            proxy_pass https://webapp;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }
    }
}
```

## Environment Variables for HTTPS

### Required for HTTPS
```bash
ASPNETCORE_URLS=https://+:8081;http://+:8080
ASPNETCORE_HTTPS_PORT=8081
ASPNETCORE_Kestrel__Certificates__Default__Password=YourCertPassword
ASPNETCORE_Kestrel__Certificates__Default__Path=/https/your-cert.pfx
```

### Force HTTPS Redirection
```bash
ASPNETCORE_HTTPS_REDIRECTION=true
ASPNETCORE_HSTS=true
```

### Update JWT for HTTPS
```bash
Jwt__Issuer=https://your-domain.com
Jwt__Audience=https://your-domain.com
```

## Testing HTTPS Connection

### Check Health Endpoint
```bash
# Test HTTPS (ignore certificate warnings for self-signed)
curl -k https://localhost:8443/health

# Test HTTP redirect
curl -L http://localhost:8080/health
```

### Check Certificate
```bash
# View certificate details
openssl s_client -connect localhost:8443 -showcerts
```

## Troubleshooting

### Certificate Warnings in Browser
For development certificates, you'll see a warning. Click "Advanced" → "Proceed to localhost (unsafe)" to continue.

### Certificate Not Found
```bash
# Check if certificate exists
docker exec gcc-webapp-https ls -la /https/

# Check certificate permissions
docker exec gcc-webapp-https ls -la /https/aspnetapp.pfx
```

### Port Already in Use
```bash
# Find process using port 8443
netstat -ano | findstr :8443

# Kill the process or use different port
docker run -p 9443:8081 ...
```

### Container Won't Start
```bash
# Check logs
docker logs gcc-webapp-https

# Common issues:
# - Wrong certificate password
# - Certificate file not found
# - Invalid certificate format
```

## Production Deployment Checklist

- [ ] Use real SSL certificate (not self-signed)
- [ ] Configure proper domain names in Jwt__Issuer and Jwt__Audience
- [ ] Set strong certificate password
- [ ] Enable HSTS and HTTPS redirection
- [ ] Configure firewall rules for ports 443 and 80
- [ ] Set up automatic certificate renewal (for Let's Encrypt)
- [ ] Use reverse proxy (Nginx/Traefik) for SSL termination
- [ ] Enable security headers in middleware
- [ ] Test SSL configuration with SSL Labs

## Files Created

1. **Dockerfile.https** - HTTPS-enabled Dockerfile with certificate support
2. **.env.https** - Environment variables for HTTPS configuration
3. **docker-compose.https.yml** - Full stack deployment with HTTPS
4. **docker-run-https.cmd** - Windows script for quick HTTPS deployment
5. **HTTPS_DEPLOYMENT_GUIDE.md** - This documentation

## Next Steps

1. Build the HTTPS image:
   ```bash
   docker build -f Dockerfile.https -t gcc-webapp:https .
   ```

2. Run with HTTPS:
   ```bash
   ./docker-run-https.cmd
   ```

3. Access the application:
   - HTTPS: https://localhost:8443
   - HTTP: http://localhost:8080 (redirects to HTTPS)

For production, replace the development certificate with a real SSL certificate from a trusted CA.