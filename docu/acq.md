**Fetch Measdata**
----
  Returns json data about the last acquired measurement data.

* **URL**

  /api/fetch

* **Method:**

  `GET`
  
*  **URL Params**

  None

* **Data Params**

  None

* **Success Response:**

  * **Code:** 200 <br />
    **Content:** 
    ```json
    {
  "1": {
    "Name": "Temperature",
    "Timestamp": "2018-11-08T15:51:28.6019666+01:00",
    "Value": 23.6,
    "Unit": "Celsius",
    "Symbol": "\u00b0C"
  },
  "2": {
    "Name": "PH value",
    "Timestamp": "2018-11-08T15:51:28.617591+01:00",
    "Value": 7.257,
    "Unit": "",
    "Symbol": ""
  },
  "3": {
    "Name": "Redox potential",
    "Timestamp": "2018-11-08T15:51:28.617591+01:00",
    "Value": 699.5,
    "Unit": "Millivolt",
    "Symbol": "mV"
  }
}
```
 
* **Error Response:**

  None

* **Sample Call:**

  ```javascript
    $.ajax({
      url: "/api/fetch",
      dataType: "json",
      type : "GET",
      success : function(r) {
        console.log(r);
      }
    });
  ```