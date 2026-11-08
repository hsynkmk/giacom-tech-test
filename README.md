# Giacom Tech Test

## Submission notes

**`main` branch: the tech test**
- **Task 1:** `GET /orders/status/{status}` returns orders with the given status, e.g. `Failed`. An unknown status returns 400.
- **Task 2:** `PUT /orders/{orderId}/status` with body `{ "status": "InProgress" }` updates the status.
  - 204 when updated
  - 400 for an unknown status
  - 404 for an unknown order
- **Task 3:** `POST /orders` with body `{ resellerId, customerId, items: [{ productId, quantity }] }` creates an order. It returns 201 with the new order, or 400 listing every validation error.
  - Validation rejects:
    - missing or empty ids
    - an empty `items` list
    - a quantity below 1
    - products that don't exist
  - New orders start as `Created`. Each item's `ServiceId` is taken from its product.
- **Task 4:** `GET /orders/profit/monthly` returns profit (quantity × (price − cost)) per month for `Completed` orders.
- **Code style:** kept the existing Controller → Service → Repository structure. The `OrderSummary` query is shared instead of duplicated, and namespaces are now file-scoped.
- **Tests:** unit tests for every new method, in `Order.Service.Tests`.
- **CI:** Additionally I added a GitHub Actions workflow builds the solution, runs the tests and builds the Docker image.

**`refactor/clean-architecture` branch: towards production-ready**
https://github.com/hsynkmk/giacom-tech-test/tree/refactor/clean-architecture
- As an extra, after completing the task on my own => I used **Claude Code** to refactor the whole service on a separate branch. I haven't tested it! 

- It adds:
  - Clean Architecture: Domain / Application / Infrastructure / Api, with one test project per layer
  - a `ServiceResult` pattern, with FluentValidation validators and ProblemDetails error responses
  - EF Core 8 with the Pomelo MySQL provider, and Guid ids instead of `byte[]`, which removes the comparison workarounds
  - a global exception handler, a `/health` endpoint, Swagger in Development, and API tests with `WebApplicationFactory`
  - Docker: the API waits for a healthy database, and the connection string comes from an environment variable

## Background
Giacom Cloud Market is a B2B e-commerce platform which allows IT companies (resellers) to buy services indirectly from major vendors (Microsoft, Symantec, Webroot etc) in high volumes at low cost. IT companies then resell the purchased services on to their customers, making a small margin. Behind Cloud Market are several microservices, one of which is an Order API much like the one we are going to work on for this test.

## Concepts
* Reseller = A customer of Giacom
* Customer = A customer of a Reseller
* Order = An order placed by a Reseller for a specific Customer
* Order Item = A service and product which belongs to an Order
* Order Status = The current state of an Order
* Product = An end-offering which can be purchased e.g. '100GB Mailbox'
* Service = The category the Product belongs to e.g. 'Email'
* Profit = The difference between Cost and Price

## Time
You should allocate approx. 2 hours to complete the tech test though it will likely take less time for more experienced engineers.

## Pre-Reqs
* Visual Studio 2022 (or compatible IDE for working with .net)
* .NET 8.0 SDK
* Git
* Docker (running Linux containers)
* Optional: MySQL Workbench / Heidi (database client)
* Optional: Postman (can also use any other API client)

## Setup
1. Clone this repository locally
2. Using a terminal, cd to the local repository and run 'docker-compose up db', which will start and seed the database
3. Open the solution file in /src
4. Start debugging or run the Order.WebAPI project then query http://localhost:8000/orders in your API client / browser to test that setup is complete. You should see orders being returned from the API
   
## Tasks
Add a new API endpoint for each of the following tasks:
1. Return Orders with a specified Order Status e.g. 'Failed'
2. Allow an Order Status to be updated to a different status e.g. 'InProgress'
3. Allow an Order to be created. This should include validation of any parameters
4. Calculate profit by month for all 'completed' Orders

Finally, once code-complete, close your IDE, run 'docker-compose down --volumes' to stop and remove the database container. Now run 'docker-compose up'. This will run the local database and also build the microservice in Release mode. Test the API is working correctly via this method (as this is the one Giacom will run to test the submission).

## Submission
Please push your code to a new github repository then send the repository link to the email address from which the tech test was issued. If applicable, add notes in the email explaining why you have chosen a particular approach.
Alternatively, zip or git-bundle the repository and email it.

## Help
If you happen to run into any issues when running the Docker container, try deselecting Hyper-V Services in "Windows Features" (Search for Windows Features in Start Menu), selecting again, and then restarting your computer.

To connect to the MySQL database directly the credentials are as follows:
* Hostname: *localhost*
* Username: *order-service*
* Password: *nmCsdkhj20n@Sa*

If you experience further issues getting set up with the tech test please reply to the email address from which the tech test was issued with your query.

Copyright (c) 2025, Giacom.
