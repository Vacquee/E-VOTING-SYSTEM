# Student Organizations Election and Voting Management System

A secure, web-based voting system built with **ASP.NET Core MVC** and **MongoDB** for student organization elections.

## Features

### Student Features
- ✅ Secure registration and login with Student ID format validation (YY-#####)
- ✅ Account verification system
- ✅ View elections (Upcoming, Ongoing, Past)
- ✅ Apply for candidacy
- ✅ Private and anonymous voting
- ✅ One Person, One Vote enforcement
- ✅ Terms and conditions acceptance before voting
- ✅ Vote confirmation screen

### Admin Features
- ✅ Manage student accounts
- ✅ Approve/reject verification requests
- ✅ Create and manage elections
- ✅ Manage positions and candidates
- ✅ Approve/reject candidate applications
- ✅ View real-time voting participation
- ✅ Generate and view election results
- ✅ Comprehensive admin dashboard

### Security Features
- ✅ Password hashing (SHA256)
- ✅ Cookie-based authentication
- ✅ Role-based authorization (Admin, Student)
- ✅ Anti-forgery token protection
- ✅ Account lockout after failed login attempts
- ✅ Audit logging for critical actions
- ✅ MongoDB unique index enforces one vote per position per student

## Technology Stack

- **Framework**: ASP.NET Core 8.0 MVC
- **Database**: MongoDB
- **Authentication**: Cookie-based authentication
- **UI**: Razor Views (.cshtml) with Bootstrap 5
- **Theme**: Professional green-and-white color scheme

## Prerequisites

Before running this project, ensure you have:

1. **.NET 8.0 SDK** installed
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0

2. **MongoDB** installed and running locally
   - Download from: https://www.mongodb.com/try/download/community
   - Default connection: `mongodb://localhost:27017`

3. **Visual Studio 2022** (optional) or **VS Code**

## Installation and Setup

### 1. Clone or Navigate to the Repository

```bash
cd c:\Users\admin\Repositories\cdm-voting-system
```

### 2. Restore NuGet Packages

```bash
dotnet restore
```

### 3. Configure MongoDB Connection (Optional)

Edit `appsettings.json` if your MongoDB is running on a different host/port:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "VotingSystemDB"
  }
}
```

### 4. Run the Application

```bash
dotnet run
```

The application will start on:
- **HTTPS**: https://localhost:5001
- **HTTP**: http://localhost:5000

### 5. Access the Application

Open your browser and navigate to:
```
https://localhost:5001
```

## Default Admin Credentials

After the first run, a default admin account is automatically created:

```
Student Number: 00-00000
Password: admin123
```

**⚠️ IMPORTANT: Change the default password immediately after first login!**

## Project Structure

```
VotingSystem/
├── Controllers/
│   ├── AccountController.cs      # Login, Register, Logout
│   ├── StudentController.cs      # Student dashboard, voting, profile
│   ├── AdminController.cs        # Admin management features
│   └── HomeController.cs         # Landing page
├── Models/
│   ├── User.cs                   # User/Student model
│   ├── Election.cs               # Election, Position, Candidate models
│   ├── Vote.cs                   # Vote model
│   ├── AuditLog.cs              # Audit logging
│   └── ViewModels.cs            # View models for forms
├── Services/
│   ├── AuthService.cs           # Authentication logic
│   ├── ElectionService.cs       # Election management
│   ├── VotingService.cs         # Voting logic with one-vote enforcement
│   ├── VerificationService.cs   # Account verification
│   └── AuditService.cs          # Audit logging
├── Data/
│   ├── MongoDbContext.cs        # MongoDB connection and collections
│   └── SeedData.cs              # Database seeding
├── Views/
│   ├── Account/                 # Login, Register views
│   ├── Student/                 # Student dashboard, voting views
│   ├── Admin/                   # Admin management views
│   ├── Home/                    # Landing page
│   └── Shared/                  # Layout and partials
├── wwwroot/
│   └── css/
│       └── site.css             # Green-and-white theme
├── Program.cs                   # Application entry point
├── appsettings.json            # Configuration
└── VotingSystem.csproj         # Project file
```

## MongoDB Collections

The system uses the following MongoDB collections:

- **users** - Student and admin accounts
- **elections** - Election information
- **positions** - Positions within elections
- **candidates** - Candidate applications
- **votes** - Anonymous vote records
- **verification_requests** - Account verification requests
- **audit_logs** - System audit trail

## Key Features Implementation

### One Vote Per Position Enforcement

The system enforces "One Person, One Vote" through:

1. **Unique Compound Index**: MongoDB index on `(electionId, positionId, voterId)` in the votes collection
2. **Backend Validation**: `VotingService` checks for existing votes before submission
3. **Duplicate Key Handling**: MongoDB rejects duplicate votes automatically

See: `Data/MongoDbContext.cs:52` and `Services/VotingService.cs:35`

### Voting Flow

The voting flow works as follows:

1. Student logs in → Dashboard
2. Views ongoing election → Election details page
3. Clicks "Vote" → Verification check
4. If verified → Vote page with terms and conditions
5. Fills ballot → Confirmation prompt
6. Accepts terms → Submits vote
7. Vote recorded → Confirmation page

See: `Controllers/StudentController.cs` and `Views/Student/Vote.cshtml`

### Account Verification Flow

1. Student registers → Not verified by default
2. Student requests verification from profile
3. Admin reviews verification request
4. Admin approves/rejects
5. If approved, student can vote and apply as candidate

See: `Services/VerificationService.cs` and `Controllers/AdminController.cs:94`

## User Workflows

### For Students

1. **Register**: Click "Sign Up" → Fill registration form with Student ID (YY-#####)
2. **Login**: Use Student Number and Password
3. **Verify Account**: Go to Profile → Request Verification → Wait for admin approval
4. **Vote**: Dashboard → Select ongoing election → Click "Vote" → Fill ballot → Accept terms → Submit
5. **Apply as Candidate**: Election page → Click "Apply as Candidate" → Fill bio → Submit

### For Admins

1. **Login**: Use admin credentials (00-00000 / admin123)
2. **Approve Verifications**: Verifications menu → Review → Approve/Reject
3. **Create Election**: Elections → Create New Election → Add positions → Set dates
4. **Approve Candidates**: Candidates menu → Review applications → Approve/Reject
5. **View Results**: Dashboard → Select past election → Click "Results"

## Database Indexes

Critical indexes created at startup (see `Data/MongoDbContext.cs:26`):

- **users.email** - Unique index
- **users.studentNumber** - Unique index
- **elections.status** - For filtering published elections
- **positions.electionId** - For fetching positions
- **votes (electionId, positionId, voterId)** - **Unique compound index** (enforces one vote per position)

## Security Considerations

### Implemented

- Password hashing (SHA256)
- Anti-CSRF tokens on all forms
- Secure cookie flags (HttpOnly, SameSite)
- Account lockout after 5 failed login attempts
- Input validation on all forms
- Role-based authorization
- Audit logging for critical actions

### Recommended for Production

- Use PBKDF2 or bcrypt for password hashing
- Enable HTTPS enforcement (HSTS)
- Implement rate limiting
- Add email verification
- Use environment variables for secrets
- Enable MongoDB authentication
- Regular database backups

## Troubleshooting

### MongoDB Connection Error

**Error**: "Unable to connect to MongoDB"

**Solution**:
1. Ensure MongoDB is running: `mongod`
2. Check connection string in `appsettings.json`
3. Verify MongoDB is listening on port 27017

### Port Already in Use

**Error**: "Address already in use"

**Solution**:
Change the port in `Properties/launchSettings.json` or run:
```bash
dotnet run --urls "https://localhost:5002;http://localhost:5001"
```

### Duplicate Key Error on Votes

**Error**: "Duplicate key error on votes collection"

**Solution**: This is expected behavior - it means the student already voted for that position. The system prevents duplicate votes.

## Future Enhancements

Possible improvements for future versions:

- Email notifications for verification and election events
- Photo upload for candidate profiles
- Real-time vote count updates (SignalR)
- Export results to PDF/CSV
- Multi-language support
- Dark mode toggle
- Voter turnout analytics
- Automated backups

## Database Backup (Manual)

To backup the MongoDB database:

```bash
mongodump --db VotingSystemDB --out ./backup
```

To restore:

```bash
mongorestore --db VotingSystemDB ./backup/VotingSystemDB
```

## Testing the System

### Quick Test Workflow

1. Start the application
2. Login as admin (00-00000 / admin123)
3. Create an election with positions
4. Register a student account (use format: 24-12345)
5. Login as admin → Approve student verification
6. Login as student → Apply as candidate
7. Login as admin → Approve candidate
8. Login as student → Vote in the election
9. Try voting again (should be rejected)
10. Login as admin → Close election → View results

## Support and Documentation

- **Code Documentation**: Inline comments explain security-critical logic
- **System Architecture**: See documentation files for implementation details

## License

This is a school project for educational purposes.

## Credits

Built with ASP.NET Core MVC and MongoDB for student organization elections.
