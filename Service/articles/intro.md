---
title: "How to use the WellBore microservice?"
output: html_document
---

Typical Usage
===
1. Upload a new WellBore using the `Post` web api method.
2. Call the `Get` method with the identifier of the uploaded WellBore as argument. 
The return Json object contains the WellBore description.
3. Optionally send a `Delete` request with the identifier of the WellBore in order to delete the WellBore if you do not 
want to keep the WellBore uploaded on the microservice.


