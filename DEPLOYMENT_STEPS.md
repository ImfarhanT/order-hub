# 🚀 Order Hub - Complete Deployment Guide

## 📋 **What You'll Get**
- ✅ **Live API** accessible from anywhere
- ✅ **Secure database** with automatic table creation  
- ✅ **WooCommerce plugin** ready to sync orders
- ✅ **Admin dashboard** for managing sites and orders
- ✅ **Production-ready** system with monitoring

---

## 🎯 **Step 1: Create GitHub Repository**

### 1.1 Go to GitHub
- Visit [github.com](https://github.com)
- Sign in or create account
- Click "New repository"

### 1.2 Create Repository
```
Repository name: order-hub
Description: Central dashboard for managing orders from multiple WooCommerce stores
Visibility: Public (or Private if preferred)
Initialize with: Don't add README (we already have one)
```

### 1.3 Push Your Code
```bash
# In your terminal, run these commands:
cd /Users/farhan/Desktop/order-hub

# Add GitHub as remote (replace YOUR_USERNAME with your actual GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/order-hub.git

# Push to GitHub
git branch -M main
git push -u origin main
```

---

## 🗄️ **Step 2: Set Up Supabase Database (Free Tier)**

### 2.1 Create Supabase Account
- Go to [supabase.com](https://supabase.com)
- Click "Start your project"
- Sign up with GitHub or email

### 2.2 Create New Project
```
Organization: Create new or use existing
Project name: order-hub-prod
Database password: Choose a strong password (save this!)
Region: Choose closest to you
Pricing plan: Free tier
```

### 2.3 Wait for Setup
- Project creation takes 2-3 minutes
- You'll see "Project is ready" when done

### 2.4 Get Connection String
- Go to **Settings** → **Database**
- Copy the connection string
- It looks like: `postgresql://postgres:[YOUR-PASSWORD]@db.xxx.supabase.co:5432/postgres`

---

## 🌐 **Step 3: Deploy to Render (Free Tier)**

### 3.1 Create Render Account
- Go to [render.com](https://render.com)
- Click "Get Started"
- Sign up with GitHub

### 3.2 Create New Web Service
- Click **"New +"** → **"Web Service"**
- Connect your GitHub account if not already connected

### 3.3 Configure Service
```
Name: order-hub
Environment: .NET
Region: Choose closest to you
Branch: main
Root Directory: hub
```

### 3.4 Build & Start Commands
```
Build Command: dotnet publish -c Release -o out
Start Command: dotnet out/HubApi.dll
```

### 3.5 Set Environment Variables
Click **"Environment"** and add these variables:

```bash
# Database Connection (replace with your Supabase connection string)
ConnectionStrings__Default=postgresql://postgres:[YOUR-PASSWORD]@db.xxx.supabase.co:5432/postgres?sslmode=require

# Security (use the key from your env.production file)
SITE_SECRETS_KEY=Ihlz4UD0w3soTVf2rB66lzecabXk/sMOcP2Ik3VcPF0=

# Admin Access
ADMIN_EMAIL=admin@orderhub.com
ADMIN_PASSWORD=OrderHub2024!Secure

# Application Settings
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80
```

### 3.6 Deploy
- Click **"Create Web Service"**
- Wait for build and deployment (5-10 minutes)
- Your hub will be available at: `https://your-app-name.onrender.com`

---

## 🧪 **Step 4: Test Your Deployment**

### 4.1 Check Health
```bash
# Test if your API is running
curl https://your-app-name.onrender.com/swagger

# Test sites endpoint
curl https://your-app-name.onrender.com/api/sites
```

### 4.2 Verify Database
- Go to your Render dashboard
- Check logs for database connection success
- You should see tables being created automatically

### 4.3 Test Admin Access
- Visit: `https://your-app-name.onrender.com/admin/login`
- Login with: `admin@orderhub.com` / `OrderHub2024!Secure`

---

## 📱 **Step 5: Install WooCommerce Plugin**

### 5.1 Upload Plugin
```bash
# Copy plugin to your WordPress site
cp -r wc-plugin /path/to/wp-content/plugins/order-hub-sync
```

### 5.2 Activate Plugin
- Go to WordPress admin → **Plugins**
- Find "Order Hub Sync"
- Click **"Activate"**

### 5.3 Configure Plugin
- Go to **WooCommerce** → **Order Hub Sync**
- Enter these settings:
  ```
  Hub API Base URL: https://your-app-name.onrender.com
  Site API Key: (get from hub admin)
  Site API Secret: (get from hub admin)
  ```

---

## 🔑 **Step 6: Create Your First Site**

### 6.1 Login to Hub Admin
- Visit: `https://your-app-name.onrender.com/admin/login`
- Use: `admin@orderhub.com` / `OrderHub2024!Secure`

### 6.2 Create Site
- Go to **Sites** section
- Click **"Add New Site"**
- Fill in:
  ```
  Name: Your Store Name
  Base URL: https://yourstore.com
  ```

### 6.3 Get API Credentials
- After creating, you'll see:
  - **API Key**: Copy this
  - **API Secret**: Copy this (shown only once!)
- Paste these into your WooCommerce plugin settings

---

## 🎉 **Step 7: Test Complete Workflow**

### 7.1 Place Test Order
- Go to your WooCommerce store
- Add item to cart and checkout
- Complete the order

### 7.2 Verify Sync
- Go to your Hub admin
- Check **Orders** section
- You should see the test order appear

### 7.3 Test Shipping Update
- Change order status in WooCommerce
- Verify it updates in Hub

---

## 🔧 **Troubleshooting**

### Common Issues

#### 1. **Build Fails**
- Check Render logs for errors
- Verify .NET 9 compatibility
- Ensure all files are committed to GitHub

#### 2. **Database Connection Fails**
- Verify Supabase connection string
- Check if database is accessible
- Ensure SSL mode is set to require

#### 3. **Plugin Can't Connect**
- Verify Hub URL is correct
- Check API key/secret match
- Enable debug logging in plugin

#### 4. **Orders Not Syncing**
- Check plugin configuration
- Verify HMAC signatures
- Check Hub logs for errors

### Get Help
- **Render Logs**: Check dashboard for errors
- **Supabase Logs**: Check SQL editor for queries
- **Plugin Logs**: Enable debug mode in WordPress

---

## 📊 **Monitor Your System**

### 1. **Render Dashboard**
- Monitor application health
- Check resource usage
- View real-time logs

### 2. **Supabase Dashboard**
- Monitor database performance
- Check connection status
- View table data

### 3. **Hub Admin**
- Monitor order sync status
- Check site health
- View revenue reports

---

## 🚀 **Next Steps After Deployment**

### 1. **Customize Settings**
- Update admin email/password
- Configure CORS for your domains
- Set up custom logging

### 2. **Add More Stores**
- Create additional sites in Hub admin
- Configure WooCommerce plugins
- Test multi-store aggregation

### 3. **Set Up Monitoring**
- Configure alerts for failures
- Set up backup procedures
- Monitor performance metrics

---

## 🎯 **Your Live URLs**

After successful deployment, you'll have:

- **Hub API**: `https://your-app-name.onrender.com`
- **Swagger Docs**: `https://your-app-name.onrender.com/swagger`
- **Admin Login**: `https://your-app-name.onrender.com/admin/login`
- **API Endpoints**: `https://your-app-name.onrender.com/api/*`

---

## 🎉 **Congratulations!**

You now have a **production-ready Order Hub system** that can:
- ✅ Aggregate orders from multiple WooCommerce stores
- ✅ Provide secure API access with HMAC authentication
- ✅ Calculate revenue shares automatically
- ✅ Track shipping updates in real-time
- ✅ Generate comprehensive reports
- ✅ Scale with your business needs

**Your Order Hub is now live and ready to use!** 🚀
