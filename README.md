# Household Budget Tracker

A shared budgeting web app for families and roommates. Everyone in a household logs expenses, sets monthly limits for each category, and sees on one dashboard how much of each budget has been used.

Built by Tyler Rusnell for **CSE 325: .NET Software Development** (BYU-Idaho).

- **Live site:** https://householdbudget.onrender.com
- **GitHub:** https://github.com/3lue7hunder/HouseholdBudget
- **Trello board:** https://trello.com/b/bxvvCb6b/cse-325-net-software-development-group-project

> The live site runs on a free Render plan. If nobody has visited for a while it goes to sleep, so the first page load can take about a minute.

## Features

- **Accounts:** register, log in and log out with ASP.NET Core Identity. Household pages require a login.
- **Households:** create a household and get an invite code, or join an existing household with a code. Members can see who else is in the household and can leave.
- **Expenses:** add, edit and delete expenses with a description, amount, date, category and optional note. Each expense records who paid. Filter by month and see the monthly total.
- **Categories:** each new household starts with default categories (Housing, Groceries, Utilities, Transportation, Dining Out, Health, Entertainment, Savings, Other). Add, rename and delete your own. A category that still has expenses cannot be deleted.
- **Budgets:** set a monthly limit for any category and watch a progress bar fill as you spend. The bar is green, turns yellow at 75% and red at 100%.
- **Dashboard:** the home page shows spent, budgeted and remaining totals for the current month, budget progress by category, spending by member and recent expenses.

## Tech Stack

| Area | Technology |
| --- | --- |
| Framework | .NET 10, Blazor Web App (Interactive Server) |
| Authentication | ASP.NET Core Identity |
| Data access | Entity Framework Core 10 |
| Database | SQLite locally, PostgreSQL in production |
| Styling | Bootstrap (included with the Blazor template) |
| Hosting | Render (Docker) |
| Source control and planning | GitHub and Trello |

## User Guide

### 1. Create an account
Click **Register** in the top bar, enter an email address and password, and submit. You are logged in right away. There is no password reset, so choose a password you will remember.

### 2. Create or join a household
Open **Household** in the side menu.
- To start a new household, enter a name and click **Create**. You will see an invite code.
- To join someone else's household, enter the invite code they gave you and click **Join**.

Share your invite code with the people you want in your household. They register their own account, then enter the code on the Household page.

### 3. Add an expense
Open **Expenses** and click **Add expense**. Fill in the description, amount, date and category (add a note if you like), then click **Save**. Use the month picker to look at other months. Use **Edit** or **Delete** on any row to change or remove an expense.

### 4. Manage categories
Open **Categories** to add a new category, rename one, or delete one. A category with expenses in it cannot be deleted.

### 5. Set budgets
Open **Budgets**. Type a monthly limit next to a category and click outside the box to save it. Clear the box to remove that budget. The progress bar shows how much of the limit has been spent in the selected month.

### 6. Check the dashboard
The home page summarizes the current month: total spent, total budgeted, what is left, progress by category, spending by member and the latest expenses.

## Project Structure

```
HouseholdBudget/
  Components/
    Account/        Login, register and other Identity pages (from the template)
    Layout/         Main layout and the navigation menu
    Pages/          Home (dashboard), Household, Expenses, Categories, Budgets
  Data/
    ApplicationDbContext.cs
    ApplicationUser.cs
    Household.cs, Category.cs, Expense.cs, Budget.cs
    Migrations/     EF Core migrations for SQLite
  Services/
    BudgetService.cs   All household, expense, category and budget logic
  Program.cs        App startup, authentication and database setup
  Dockerfile        Used for the cloud deployment
```

Every query in `BudgetService` is limited to the signed-in user's household, so one household can never read or change another household's data.

## Known Limitations and Next Steps

- Any household member can edit or delete any expense in their household. The planned rule that members can only change their own expenses is not built.
- There is no admin role, so members cannot remove other members. They can only leave on their own.
- There is no multi-month spending history or trends page. Single months can be viewed on the Expenses and Budgets pages.
- There is no forgot-password option. Accounts use email and password only, with no email confirmation, so a user who forgets their password cannot reset it.
- A user belongs to one household at a time.

## Author

**Tyler Rusnell**: designed and built the application, including the data model, authentication, household sharing, expense, category and budget features, the dashboard, the Render deployment, and the project documentation.

## License

This project was created for a course assignment at BYU-Idaho.