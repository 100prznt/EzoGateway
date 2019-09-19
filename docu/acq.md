# Acquire new Measdata

Perform a new hardware measurement. The masdata can fetched with the `api\fetch` call.

### URL
/api/aqc

### Method
`GET`
  
### URL Params
None

### Data Params
None

### Success Response

#### Code
`200 OK`

#### Content 
```javascript
{
  "task": {
    "id": 1,
    "Name": "AcquireMeasdata",
    "href": ""
  }
}
```
 
### Error Response
None

### Sample Call
  ```javascript
    $.ajax({
      url: "/api/acq",
      dataType: "json",
      type : "GET",
      success : function(r) {
        console.log(r);
      }
    });
  ```