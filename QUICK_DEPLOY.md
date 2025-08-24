# 🚀 Quick Deployment Checklist

## 🔑 **Your Credentials (Save These!)**
```
Encryption Key: Ihlz4UD0w3soTVf2rB66lzecabXk/sMOcP2Ik3VcPF0=
Admin Email: admin@orderhub.com
Admin Password: OrderHub2024!Secure
```

## 📋 **Deployment Steps (15 minutes)**

### ✅ **Step 1: GitHub (2 min)**
- [ ] Go to [github.com](https://github.com)
- [ ] Create new repository: `order-hub`
- [ ] Run: `git remote add origin https://github.com/YOUR_USERNAME/order-hub.git`
- [ ] Run: `git push -u origin main`

### ✅ **Step 2: Supabase Database (5 min)**
- [ ] Go to [supabase.com](https://supabase.com)
- [ ] Create account & new project: `order-hub-prod`
- [ ] Save database password
- [ ] Copy connection string from Settings → Database

### ✅ **Step 3: Render Deployment (8 min)**
- [ ] Go to [render.com](https://render.com)
- [ ] Sign up with GitHub
- [ ] Create new Web Service
- [ ] Connect your `order-hub` repository
- [ ] Set root directory to `hub`
- [ ] Add environment variables (see below)
- [ ] Deploy!

## 🌐 **Environment Variables for Render**

```bash
ConnectionStrings__Default=postgresql://postgres:[YOUR-PASSWORD]@db.xxx.supabase.co:5432/postgres?sslmode=require
SITE_SECRETS_KEY=Ihlz4UD0w3soTVf2rB66lzecabXk/sMOcP2Ik3VcPF0=
ADMIN_EMAIL=admin@orderhub.com
ADMIN_PASSWORD=OrderHub2024!Secure
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80
```

## 🎯 **After Deployment**

1. **Test API**: Visit `https://your-app.onrender.com/swagger`
2. **Login Admin**: `https://your-app.onrender.com/admin/login`
3. **Create Site**: Get API key/secret for WooCommerce plugin
4. **Install Plugin**: Upload `wc-plugin` to WordPress
5. **Test Order**: Place test order, verify sync

## 🆘 **Need Help?**

- **Full Guide**: See `DEPLOYMENT_STEPS.md`
- **Deploy Script**: Run `./deploy.sh`
- **Check Logs**: Render dashboard → your service → logs

## 🎉 **You're Ready!**

Your Order Hub will be live at: `https://your-app-name.onrender.com`
