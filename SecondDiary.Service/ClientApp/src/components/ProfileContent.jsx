import React, { useState, useEffect } from 'react';
import { useMsal } from '@azure/msal-react';
import { graphConfig, loginRequest } from '../authConfig';

export const ProfileContent = () => {
  const { instance, accounts } = useMsal();
  const [graphData, setGraphData] = useState(null);

  const name = accounts[0] && accounts[0].name;

  useEffect(() => {
    if (accounts[0]) {
      // Get an access token for Microsoft Graph
      instance.acquireTokenSilent({
        ...loginRequest,
        account: accounts[0]
      }).then((response) => {
        // Call the Microsoft Graph API with the token
        callMsGraph(response.accessToken);
      }).catch(error => {
        // Handle error
        console.log(error);
      });
    }
  }, [accounts, instance]);

  const callMsGraph = (accessToken) => {
    fetch(graphConfig.graphMeEndpoint, {
      headers: {
        Authorization: `Bearer ${accessToken}`
      }
    })
    .then(response => response.json())
    .then(data => setGraphData(data))
    .catch(error => console.log(error));
  };

  return (
    <div>
      <h5 className="card-title">Welcome {name}</h5>
    </div>
  );
};
