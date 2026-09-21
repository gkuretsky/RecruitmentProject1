# RecruitmentProject - Authentication & Database Reference Guide

This document explains how login/registration, the database, and email work in this
project, and how to test, tweak, and eventually deploy them.

---

## 1. Overview of what was added

- **ASP.NET Core Identity** handles user accounts (register, login, logout, password reset).
- **Database**: SQLite file `app.db`, managed via **Entity Framework Core**.
- **Site-wide login requirement**: every page requires an authenticated user, except the
  Identity account pages themselves (Login, Register, Forgot/Reset Password, etc.).
- **Email**: password reset / confirmation links are sent via `EmailSender`
  (`Services/EmailSender.cs`). If SMTP isn't configured, the email is just logged instead
  of sent (safe for local testing).

---

## 2. Key files

| File | Purpose |
|---|---|
| `Data/ApplicationDbContext.cs` | EF Core DB context, extends `IdentityDbContext<IdentityUser>` |
| `appsettings.json` | Connection string (`ConnectionStrings:DefaultConnection`) and `EmailSender` SMTP settings |
| `Program.cs` | Wires up EF Core + SQLite, Identity, authentication/authorization middleware, and the global "must be logged in" fallback policy |
| `Services/EmailSender.cs` | Sends confirmation/reset emails (or logs them if SMTP isn't set up) |
| `Areas/Identity/Pages/Account/*` | Login, Register, Logout, ForgotPassword, ForgotPasswordConfirmation, ResetPassword, ResetPasswordConfirmation, ConfirmEmail, Lockout, AccessDenied — all styled to match the rest of the site |
| `Migrations/` | EF Core migration history (schema changes over time) |
| `app.db` | The actual SQLite database file (created next to the project when you run it) |

---

## 3. How the database works right now

- Database engine: **SQLite** (a single file, no server/install needed — good for a small team and free).
- Location: `RecruitmentProject/app.db` (created automatically the first time migrations are applied).
- Connection string (in `appsettings.json`):
  ```json
  "ConnectionStrings": {
	"DefaultConnection": "Data Source=app.db"
  }
  ```
- Schema: created by the migration `Migrations/20260921143845_InitialIdentitySchema.cs`,
  which has already been applied — the standard Identity tables exist:
  `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`,
  `AspNetUserTokens`, `AspNetRoleClaims`.

### Viewing/editing the data directly
Since it's just a file, you can inspect it with any SQLite browser tool, e.g.:
- [DB Browser for SQLite](https://sqlitebrowser.org/) (free, GUI) — open `app.db` directly.
- Or from the command line: `dotnet tool install --global dotnet-ef` (already installed) plus a SQLite CLI if you want raw SQL access.

### Making schema changes later (e.g., adding PNM/member tables)
Whenever you add or change a model class that's part of `ApplicationDbContext`:
```powershell
cd RecruitmentProject
dotnet ef migrations add <DescriptiveMigrationName>
dotnet ef database update
```
- `migrations add` generates a new file under `Migrations/` describing the change.
- `database update` actually applies it to `app.db`.
- Never edit an already-applied migration file after it's been shared/committed — add a new migration instead.

### Resetting the database (start over during testing)
If you want a clean slate while testing registration:
```powershell
cd RecruitmentProject
# stop the app first
Remove-Item .\app.db
dotnet ef database update
```
This deletes all registered users and recreates an empty schema.

---

## 4. How login/registration works

- **Register**: `/Identity/Account/Register` — creates a new `IdentityUser`, signs them in immediately.
- **Login**: `/Identity/Account/Login` — email + password, optional "remember me".
- **Logout**: triggered via the "Log Out" button in the sidebar (only visible when logged in).
- **Forgot password**: `/Identity/Account/ForgotPassword` — generates a reset token and
  "sends" (or logs) an email with a reset link.
- **Reset password**: link from the email goes to `/Identity/Account/ResetPassword`.
- **Site-wide protection**: configured in `Program.cs` via:
  ```csharp
  builder.Services.AddAuthorization(options =>
  {
	  options.FallbackPolicy = new AuthorizationPolicyBuilder()
		  .RequireAuthenticatedUser()
		  .Build();
  });
  ```
  This means *any* page not explicitly marked `[AllowAnonymous]` requires login.
  All the Identity account pages are marked `[AllowAnonymous]` in their `.cshtml.cs` files
  so you can actually reach Login/Register without already being logged in.

---

## 5. How to test everything locally

1. **Run the app** (F5 in Visual Studio, or from the project folder: `dotnet run`).
2. Visiting any page (e.g. `/`) while logged out should **redirect you to `/Identity/Account/Login`**.
3. Click "Create an account" → fill out Register form → you should be signed in automatically
   and redirected to the Home page.
4. Click "Log Out" in the sidebar → you should be signed out and sent back to Login.
5. Log back in with the same email/password.
6. Try "Forgot your password?":
   - Since SMTP isn't configured yet, check the **terminal/Output window logs** — the
	 reset link will be logged there (search for "Logging email instead of sending it").
   - Copy that link into your browser to actually reset the password.
7. If something looks wrong styling-wise, the Identity pages reuse the same CSS classes
   as the rest of the site (`hero-card`, `panel-card`, `panel-label`, `panel-select`,
   `panel-validation`, `btn-belle`) — tweak those in `wwwroot/css/site.css` and it will
   affect both the Identity pages and the rest of the site consistently.

---

## 6. Configuring real email (for production, or to actually test email delivery)

Edit the `EmailSender` section in `appsettings.json` (or better, use user-secrets / environment
variables so real credentials never get committed to git):

```json
"EmailSender": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "UserName": "youraddress@gmail.com",
  "Password": "your-app-password",
  "FromAddress": "youraddress@gmail.com",
  "FromName": "Ole Miss Recruitment"
}
```

- **Gmail**: you must use an "App Password" (not your normal password) — requires
  2-Step Verification enabled on the Google account, then generate one at
  https://myaccount.google.com/apppasswords.
- **SendGrid** (free tier, 100 emails/day): use `smtp.sendgrid.net`, port 587,
  username is literally `apikey`, password is your SendGrid API key.
- To keep secrets out of `appsettings.json` while developing, run from the project folder:
  ```powershell
  dotnet user-secrets init
  dotnet user-secrets set "EmailSender:UserName" "youraddress@gmail.com"
  dotnet user-secrets set "EmailSender:Password" "your-app-password"
  ```
  User secrets automatically override `appsettings.json` locally and are never committed to git.

---

## 7. Deploying / going live checklist

- [ ] Make sure the hosting environment (IIS app pool identity, etc.) has **write permission**
	  to the folder containing `app.db` (SQLite needs to write to it).
- [ ] Configure real SMTP settings via environment variables or a secrets manager — do NOT
	  commit real credentials to `appsettings.json` in git.
- [ ] Consider whether SQLite is still right for you at that point — it's fine for a small
	  team, but if you expect heavy concurrent traffic, an Azure SQL Database (has a free/low-cost
	  tier) is easy to switch to later (see section 3 in the earlier conversation: swap
	  `UseSqlite` → `UseSqlServer`, update the connection string, re-run migrations).
- [ ] Copy `app.db` (or your production DB) somewhere backed up periodically.
- [ ] Double check `ASPNETCORE_ENVIRONMENT` is set to `Production` on the server so
	  detailed error pages aren't shown to real users (already handled by
	  `if (!app.Environment.IsDevelopment())` in `Program.cs`).

---

## 8. Quick command reference

```powershell
# Run the app locally
cd RecruitmentProject
dotnet run

# Add a new migration after changing a model
dotnet ef migrations add <Name>

# Apply migrations to the database
dotnet ef database update

# List all migrations and whether they're applied
dotnet ef migrations list

# Remove the last (unapplied) migration if you made a mistake
dotnet ef migrations remove

# Reset the local database completely
Remove-Item .\app.db
dotnet ef database update
```
