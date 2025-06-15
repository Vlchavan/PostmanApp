// Original fetch:
// const response = await fetch(url, requestOptions);

// Modified fetch to call your .NET Core MVC proxy:
const proxyUrl = 'https://localhost:7001/api/proxy/send'; // Replace with your actual backend URL

const proxyRequestOptions = {
    method: 'POST', // The proxy method is always POST to send the request model
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify({ // Send the actual request details in the body to your proxy
        url: url,
        method: method,
        headers: headers,
        body: requestBody
    })
};

// Make the request to your backend proxy
const proxyResponse = await fetch(proxyUrl, proxyRequestOptions);

// Assuming your proxy returns a JSON object like { StatusCode, StatusPhrase, Headers, Body, ContentType }
const proxyResponseBody = await proxyResponse.json();

// Now process the proxied response
rawResponsePre.textContent = proxyResponseBody.Body;
responseStatusDiv.textContent = `Status: ${proxyResponseBody.StatusCode} ${proxyResponseBody.StatusPhrase}`;
responseTimeDiv.textContent = `Time: ${timeTaken} ms`; // Time taken for frontend to backend + backend to target

// ... rest of your JSON/XML/HTML parsing logic using proxyResponseBody.Body and proxyResponseBody.ContentType