# Household Budget: setup steps

Project: HouseholdBudget (.NET 10 Blazor Web App, Individual accounts, Server interactivity)

## 1. Copy files into your project

Copy these into your HouseholdBudget project folder (keep the same folder names):

- Data/ApplicationUser.cs (replace the existing file)
- Data/ApplicationDbContext.cs (replace the existing file)
- Data/Household.cs, Data/Category.cs, Data/Budget.cs, Data/Expense.cs (new)
- Services/BudgetService.cs (new folder and file)
- Program.cs (replace; your existing file with the changes from step 3 already made)
- Components/Pages/Home.razor (replace)
- Components/_Imports.razor and Components/Layout/NavMenu.razor (replace; these are your existing files with my additions, so steps 2 and 4 below are already done in them)
- Components/Pages/Household.razor, Expenses.razor, Categories.razor, Budgets.razor (new)

Delete the template demo pages: Components/Pages/Counter.razor, Weather.razor, Auth.razor.
Keep Error.razor and NotFound.razor.

Note: your template uses per-page render modes, so each interactive page here declares
`@rendermode InteractiveServer` right under its @page line. Home.razor is read-only and runs as static server rendering.

## 2. Components/_Imports.razor

Add these three lines at the bottom:

    @using Microsoft.AspNetCore.Authorization
    @using HouseholdBudget.Data
    @using HouseholdBudget.Services

## 3. Program.cs changes

Add this using at the top:

    using HouseholdBudget.Services;

Find the lines that register the database:

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));

Replace them with:

    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
    builder.Services.AddScoped(sp =>
        sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
    builder.Services.AddScoped<BudgetService>();

In AddIdentityCore, set RequireConfirmedAccount to false so people can log in
without an email sender:

    (inside the existing options block, change RequireConfirmedAccount = true to false and keep
    the SchemaVersion line)

Right after `var app = builder.Build();` add this so the database is created automatically
(this also helps when you deploy):

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }

## 4. Nav menu

In Components/Layout/NavMenu.razor, add these inside the nav, below the Home item
(follow the same pattern as the existing items):

    <div class="nav-item px-3">
        <NavLink class="nav-link" href="expenses">Expenses</NavLink>
    </div>
    <div class="nav-item px-3">
        <NavLink class="nav-link" href="budgets">Budgets</NavLink>
    </div>
    <div class="nav-item px-3">
        <NavLink class="nav-link" href="categories">Categories</NavLink>
    </div>
    <div class="nav-item px-3">
        <NavLink class="nav-link" href="household">Household</NavLink>
    </div>

Also remove the nav items for Counter, Weather and Auth Required.

## 5. Recreate the database migration

The template ships with an Identity-only migration, and we added tables and a column on the
user, so start fresh. From the HouseholdBudget project folder:

    dotnet tool install --global dotnet-ef
    (delete the Data/Migrations folder and app.db, app.db-shm, app.db-wal if they exist)
    dotnet ef migrations add InitialCreate -o Data/Migrations
    dotnet run

## 6. Try it

1. Register an account, then open Household and create a household (default categories are added).
2. Add expenses, set budgets, check the dashboard on the home page.
3. Register a second account in a private window and join with the invite code.

## Notes for deployment

SQLite stores data in app.db, which is wiped on hosts with temporary disks. For a real
cloud deployment, switch to a hosted database (Azure SQL, PostgreSQL) by changing the provider
package, UseSqlite call and connection string, then recreate the migration.
