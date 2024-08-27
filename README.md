### Expected Behavior

When we have a Exception happening in an Activity (in this example, we have an HTTPResponseException) the expectation would be the exception would be caught in the Workflow and then the result object would be returned with the assigned error.

### What do we see

Executing the tests show this doesn't seem to be the case and the error information is lost after when the Workflow returns the object.

If we look at how the code is executed, we are simulating two cases:

* Happy Path where everything goes well
* WithError, where we have an Exception being thrown.

In the Happy Path execution everything goes well and the result I get after the Workflow is executed is the expected.

````
=-=-=-=-=-=-=-=-=-=-=-=-=- Starting Happy Path =-=-=-=-=-=-=-=-=-=-=-=-=-

HappyPath inside Workflow
Data: Good Data
IsValid: True

=-=-=-=-=-=-=-=-=-=-=-=-

Happy Path after finishing
Data: Good Data
IsValid: True

=-=-=-=-=-=-=-=-=-=-=-=-
````

At the end, we end up with the expected object, containing the expected data.

However, when executing the WithError flow, before returning the result object, we have the correct values, however, after the object is returned, we have a "clean" instance of the Result class.

````
=-=-=-=-=-=-=-=-=-=-=-=-=- Starting WithError =-=-=-=-=-=-=-=-=-=-=-=-=-
With error inside Workflow
Data:
IsValid: False
Error: Activity failed: Activity task failed

=-=-=-=-=-=-=-=-=-=-=-=-

With Error after finishing
Data:
IsValid: True

=-=-=-=-=-=-=-=-=-=-=-=-
````

### How to reproduce:

* Running the tests in the Tests.cs file should allow you to see the errors. We still have a Util function that will print the status of the Result object inside the Workflow.