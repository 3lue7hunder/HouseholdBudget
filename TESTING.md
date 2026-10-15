# Testing Checklist

Manual quality assurance checklist for the Household Budget Tracker. Run it on the live site
(https://householdbudget.onrender.com) and on a local copy. Mark each row Pass or Fail and note the date.

Use two accounts in two browser windows (one normal, one private) for the household tests.

## Accounts
| # | Test | Expected result | Result |
| --- | --- | --- | --- |
| 1 | Register with a new email and password | Account is created and you are logged in | |
| 2 | Register with an email that already exists | A clear error message is shown | |
| 3 | Register with a short password | A clear error message is shown | |
| 4 | Log out, then log in with the same account | You are logged in | |
| 5 | Log in with a wrong password | An error message is shown and you stay logged out | |
| 6 | Open /expenses while logged out | You are sent to the login page | |

## Households
| # | Test | Expected result | Result |
| --- | --- | --- | --- |
| 7 | Create a household with a name | Invite code is shown and default categories exist | |
| 8 | Create a household with an empty name | A validation message is shown | |
| 9 | Join with a wrong invite code (second account) | "No household was found" message | |
| 10 | Join with the correct invite code (second account) | Second account sees the same household | |
| 11 | Leave the household, then join again | Leaving and rejoining both work | |

## Expenses
| # | Test | Expected result | Result |
| --- | --- | --- | --- |
| 12 | Add an expense with all fields | Expense appears in the list and the total updates | |
| 13 | Save an expense with no description, or an amount of 0 | A validation message is shown and nothing is saved | |
| 14 | Edit an expense | Changes appear in the list | |
| 15 | Delete an expense (Delete, then Confirm) | Expense is removed and the total updates | |
| 16 | Change the month picker | Only that month's expenses are listed | |
| 17 | Add an expense with the first account, view it with the second | Both accounts see it, and "Paid by" shows the right person | |

## Categories
| # | Test | Expected result | Result |
| --- | --- | --- | --- |
| 18 | Add a new category | It appears in the list and in the expense form | |
| 19 | Add a category with a name that already exists | A "already exists" message is shown | |
| 20 | Rename a category | The new name shows everywhere | |
| 21 | Delete a category that has expenses | Refused with a message | |
| 22 | Delete an empty category | It is removed | |

## Budgets and dashboard
| # | Test | Expected result | Result |
| --- | --- | --- | --- |
| 23 | Set a limit on a category | "Budget saved" message and the progress bar appears | |
| 24 | Spend over 75% of a limit | Bar turns yellow | |
| 25 | Spend 100% or more of a limit | Bar turns red and says "over budget" | |
| 26 | Clear a limit | Budget is removed | |
| 27 | Open the home page | Spent, budgeted and remaining totals match the expenses; amounts show a $ sign | |
| 28 | Check "Spending by member" with two accounts | Each person's total is correct | |

## Data separation
| # | Test | Expected result | Result |
| --- | --- | --- | --- |
| 29 | Create a second household with a different account | It does not show the first household's expenses | |

## Layout and accessibility
| # | Test | Expected result | Result |
| --- | --- | --- | --- |
| 30 | Resize the browser to phone width | Menu collapses and tables scroll sideways instead of breaking the page | |
| 31 | Use only the Tab and Enter keys on every page | Every button and field can be reached and used, with a visible focus outline | |
| 32 | Run Lighthouse (Chrome DevTools) on the live site | Record the Performance, Accessibility, Best Practices and SEO scores | |
| 33 | Run the axe DevTools extension on each page | Record any issues and fix them | |
| 34 | Check the pages with the W3C HTML validator | Record any errors and fix them | |
