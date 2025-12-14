// Copyright 2025 Ben Voß
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files
// (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
"use strict";

// Load the SignalR script
var script = document.createElement("script");
script.src = "https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/9.0.6/signalr.min.js";
document.head.appendChild(script);

// Config for the maximum number of messages to dispay for streams and notifications
const maxHubMessages = 3;

const SignalRPlugin = function (system) {

  const _makeHub = function (hubActions, hubUrl, hubMethods) {

    const getAccessToken = () => {
      const state = system.getState().get("auth").get("authorized");

      if (!state) {
        return undefined;
      }

      const authState = state.toJS();
      const values = Object.keys(authState);

      if (values.length === 0) {
        return undefined;
      }
       
      const authConfig = values[0];

      if (authConfig.schema.type === "http" && authConfig.schema.scheme === "basic") {
        return btoa(authConfig.value.username + ":" + authConfig.value.password);
      } else  {
        return authConfig.value;
      }
    }

    // Build and start the SignalR connection
    var hub = new signalR
      .HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: getAccessToken })
      .withAutomaticReconnect()
      .build();
    hubActions.setHub({hubUrl, hub});

    hubActions.setStatus({hubUrl, status: "connecting"});

    hubMethods.forEach(method => {
      hub.on(method, function (message) {
        hubActions.addMessage(
          {
            path: hubUrl + "!" + method,
            message: JSON.stringify(message),
            type: "receive",
          });
        });
    });
    
    hub.onreconnecting(error => {
      console.assert(hub.state === signalR.HubConnectionState.Reconnecting);
      hubActions.setStatus({hubUrl, status: "reconnecting"});
    });

    hub.onreconnected(error => {
        console.assert(hub.state === signalR.HubConnectionState.Connected);
        hubActions.setStatus({hubUrl, status: "connected"});
    });

    // Start the hub connection
    return hub.start()
      .then(function () {
        hubActions.setStatus({hubUrl, status: "connected"});
        return hub;
      }).catch(function (err) {
        console.error(hubUrl, err.toString());
        throw err;
    });
  };

  const _stringify = (thing) => {
    if (typeof thing === "string") {
      return thing;
    }

    if (thing && thing.toJS) {
      thing = thing.toJS();
    }

    if (typeof thing === "object" && thing !== null) {
      try {
        return JSON.stringify(thing, null, 2);
      } catch (e) {
        return String(thing);
      }
    }

    if (thing === null || thing === undefined) {
      return "";
    }

    return thing.toString();
  };

  return {
    statePlugins: {
      spec: {
        wrapSelectors: {
          // put = stream from server
          // get = notification from server to client
          // post = method call to the hub

          // Hide the "execute" button for SignalR hub notifications
          allowTryItOutFor: (ori) => (_, path, schema) => {
            return path.indexOf("!") === -1 || schema === "post" || schema === "put";
          },
        },
        wrapActions: {
          // Handle execute button presses after arguments have been validated.
          executeRequest: (oriAction, system) => async (args) => {
            const { hubActions, specActions, hubSelectors } = system;
            const { pathName, method } = args;

            // Examine the operation to see if it's an attempt to open a connection to a SignalR hub
            if (pathName.indexOf("!") < 0) {
              return oriAction(args);
            }

            // Either invoke the hub method or start a stream
            const [ hubUrl, hubMethod ] = pathName.split('!');

            // Ensure the hub is connected
            let hub = hubSelectors.getHub(hubUrl);
            if (!hub) {
              const paths = system.spec().toJS().json.paths;

              // Filter the paths to the ones that start with the hub url
              const hubPaths = Object.keys(paths).filter(p => p.startsWith(hubUrl));

              // Get the method names from each hubPath as a list
              const hubMethods = hubPaths.map(p => p.substring(p.indexOf("!") + 1));

              hub = await _makeHub(hubActions, hubUrl, hubMethods);
            }

            // Build parameters list from the args.parameters object properties
            const paramValues = [];
            const parameterSchema = args.operation.get("parameters").toJS();
            for (let index = 0; index < parameterSchema.length; index++) {
              const paramSchema = parameterSchema[index];
              if (args.parameters.hasOwnProperty("query." + paramSchema.name)) {
                const paramValue = args.parameters["query." + paramSchema.name];

                switch (paramSchema.schema.type) {
                  case 'integer': {
                    paramValues.push(parseInt(paramValue, 10));
                    break;
                  }

                  case 'boolean': {
                    paramValues.push(paramValue === "true");
                    break;
                  }
                  
                  case 'number': {
                    paramValues.push(parseFloat(paramValue));
                    break;
                  }

                  case 'object': {
                    paramValues.push(JSON.parse(paramValue));
                    break;
                  }

                  default: {
                    paramValues.push(paramValue);
                    break;
                  }
                }
              }
            }

            const isStream = args.method === "put";
            if (isStream) {

              // Setup a stream
              const subscription = hub
                .stream(hubMethod, ...paramValues)
                .subscribe({
                    next: (item) => {
                      // setting a response makes the loading spinner stop
                      specActions.setResponse(pathName, method, {
                        headers: "",
                        status: 200,
                        url: pathName,
                        text: "",
                      });

                      hubActions.addMessage({
                        path: pathName,
                        message: JSON.stringify(item),
                        type: "receive",
                      });
                    },
                    complete: () => {
                      // setting a response makes the loading spinner stop
                      specActions.setResponse(pathName, method, {
                        headers: "",
                        status: 200,
                        url: pathName,
                        text: "",
                      });

                      hubActions.addMessage({
                        path: pathName,
                        message: "Stream completed",
                        type: "receive",
                      });
                    },
                    error: (err) => {
                      // setting a response makes the loading spinner stop
                      specActions.setResponse(pathName, method, {
                        headers: "",
                        status: 200,
                        url: pathName,
                        text: "",
                      });

                      hubActions.addMessage({
                        path: pathName,
                        message: JSON.stringify(err),
                        type: "receive",
                      });
                    },
                  });

              hubActions.setStreamSubscription({
                path: pathName,
                subscription
              });

            } else {
              hub.invoke(hubMethod, ...paramValues).then((result) => {
                hubActions.addMessage({
                  path: pathName,
                  message: paramValues,
                  type: "send"
                });
                specActions.setMutatedRequest(pathName, method, {url: pathName});
                specActions.setResponse(pathName, method, {
                  headers: "",
                  status: 200,
                  url: pathName,
                  text: JSON.stringify(result),
                });
              }
              ).catch(err => {
                console.error("Error invoking SignalR method:", hubMethod, err.toString());

                specActions.setResponse(pathName, method, {
                  headers: "",
                  status: 500,
                  url: pathName,
                  text: err.toString(),
                });
              });
            }

            return null;
          },
          // Intercept the 'Clear' button click and remove all the received messages
          clearRequest: (oriAction, system) => (path, method) => {
            const {hubActions} = system;

            if (path.indexOf('!') < 0 || method !== "put") {
              oriAction(path, method);
            }

            hubActions.clearMessages({path});
          },
        },
      },
      hub: {
        actions: {
          setHub: ({hubUrl, hub}) => {
            return {
              type: "SET_HUB",
              payload: {hubUrl, hub},
            };
          },
          setStatus: ({hubUrl, status}) => {
            return {
              type: "SET_HUB_STATUS",
              payload: {hubUrl, status},
            };
          },
          addMessage: ({path, message, type}) => {
            return {
              type: "ADD_HUB_MESSAGE",
              payload: {path, message, type},
            };
          },
          clearMessages: ({path}) => {
            return {
              type: "CLEAR_HUB_MESSAGES",
              payload: {path},
            };
          },
          setStreamSubscription: ({path, subscription}) => {
            return {
              type: "SET_HUB_STREAM_SUBSCRIPTION",
              payload: {path, subscription},
            };
          },
        },
        reducers: {
          SET_HUB: (state, {payload}) => {
            const {hubUrl, hub} = payload;
            const hubs = state.get("hubs") || {};
            return state.set("hubs", {...hubs, [hubUrl]: hub});
          },
          SET_HUB_STATUS: (state, {payload}) => {
            const {hubUrl, status} = payload
            const statuses = state.get("statuses") || {};
            return state.set("statuses", {...statuses, [hubUrl]: status});
          },
          ADD_HUB_MESSAGE: (state, {payload}) => {
            const {path, message, type} = payload;
            const messages = state.get("messages") || {};

            if (!(path in messages)) {
              messages[path] = [];
            }

            // Add the new message to the start of the messages array
            messages[path].unshift({message, type, timestamp: new Date()});

            // Remove the last message if we have reached capacity
            if (maxHubMessages && messages[path].length > maxHubMessages) {
              messages[path].pop();
            }

            return state.set("messages", {...messages});
          },
          CLEAR_HUB_MESSAGES: (state, {payload}) => {
            const {path} = payload;
            const messages = state.get("messages") || {};
            if (path in messages) {
              messages[path] = [];
            }

            return state.set("messages", {...messages});
          },
          SET_HUB_STREAM_SUBSCRIPTION: (state, {payload}) => {
            const {path, subscription} = payload;
            const subscriptions = state.get("streamSubscriptions") || {};
            return state.set("streamSubscriptions", {...subscriptions, [path]: subscription});
          },
        },
        selectors: {
          // Get the hub object for a given path
          getHub: (state, hubUrl) => {
            const hubs = state.get("hubs") || {};
            return hubs[hubUrl];
          },
          // Get the status of the hub connection for a given path
          getHubStatus: (state, hubUrl) => {
            const statuses = state.get("statuses") || {};
            return statuses[hubUrl] || "closed";
          },
          // Get the messages for a given hub path
          getHubMessages: (state, path) => {
            const messages = state.get("messages") || {};
            return messages[path] || [];
          },
          // Get the stream subscripton for a given hub path
          getStreamSubscription: (state, path) => {
            const streamSubscriptions = state.get("streamSubscriptions") || {};
            return streamSubscriptions[path];
          }
        },
      }
    },

    components: {
      SignalRMessage: ({response}) => {
        const { React } = system;
        const { content, timestamp } = response;
        const highlightCodeComponent = system.getComponent("HighlightCode", true);

        const downloadName = "response_" + new Date().getTime();
        let body;
        try {
          body = JSON.stringify(JSON.parse(content), null, "  ");
        } catch (error) {
          body = "Can't parse JSON.  Raw result:\n\n" + content;
        }

        return React.createElement(
          "tr",
          {},
          React.createElement("h5", {}, timestamp.toISOString()),
          React.createElement(highlightCodeComponent, {language: "json", canCopy: true, downloadable: true, downloadName}, body)
        );
      },
      SignalRResponse: ({response, props}) => {
        const {React} = system;

        const status = response.get("status");
        const content = response.get("text");

        const style = status == 200 
          ? {padding: 6, display: "inline-block" }
          : {padding: 6, display: "inline-block", background: "#f93e3e"};

        const downloadName = "response_" + new Date().getTime();
        let body;
        try {
          body = JSON.stringify(JSON.parse(content), null, "  ");
        } catch (error) {
          body = "Can't parse JSON.  Raw result:\n\n" + content;
        }
        
        const highlightCodeComponent = system.getComponent("HighlightCode", true);

        return React.createElement(
          "tr",
          {},
          React.createElement(
            "td", 
            {class: "response-col_event"},
            React.createElement(
              "span",
              {
                class: "opblock-summary-method",
                style
              },
              status == 200 ? "OK" : "ERROR"),
          ),
          React.createElement("td", {class: "response-col_event", style: {width: "100%"}}, 
            React.createElement(highlightCodeComponent, {language: "json", canCopy: content != null, downloadable: content != null, downloadName}, content)
          )
        );
      },
      SignalRResponses: ({hubActions, hubSelectors, specSelectors, ...props}) => {
        const {React} = system;
        const {path, method} = props;

        const hubUrl = path.substring(0, path.indexOf('!'));
        const messages = hubSelectors.getHubMessages(path);
        const status = hubSelectors.getHubStatus(hubUrl);
        const isConnected = status === "connected";

        let rows = [];
        if (method === "get" || method === "put") {
          const signalRMessageComponent = system.getComponent("SignalRMessage");

          rows = messages.map(x => React.createElement(signalRMessageComponent, {
            response: {
              url: path,
              contentType: "text/plain",
              headers: [],
              content: x.message,
              timestamp: x.timestamp
            }
          }));
        } else {
          const signalRResponseComponent = system.getComponent("SignalRResponse");

          const response = specSelectors.responseFor(path, method);
          if (response != undefined) {
            rows = [React.createElement(signalRResponseComponent, {response})];
          }
        }

        const connectHub = async () => {
          let hub = hubSelectors.getHub(hubUrl);
          if (!hub) {
            const paths = system.specSelectors.paths().toJS();

            // Filter the paths to the ones that start with the hub url
            const hubPaths = Object.keys(paths).filter(p => p.startsWith(hubUrl));

            // Get the method names from each hubPath as a list
            const hubMethods = hubPaths.map(p => p.substring(p.indexOf("!") + 1));

            hub = await _makeHub(hubActions, hubUrl, hubMethods);
          }
        }

        const disconnectHub = async (params) => {
          const hub = hubSelectors.getHub(hubUrl);
          if (hub) {
            await hub.stop();

            hubActions.setStatus({hubUrl, status: "disconnected"});
            hubActions.setHub({hubUrl, undefined});
          }
        }

        const clearMessages = async (params) => {
          hubActions.clearMessages({path});
        }

        const buttonLabel = isConnected ? "Disconnect" : "Connect";

        let buttonGroup;
        if (messages.length > 0) {
          buttonGroup = React.createElement(
            "div",
            {class: "btn-group"},
            React.createElement(
              "button",
              {
                class: "btn execute opblock-control__btn",
                onClick: isConnected ? disconnectHub : connectHub
              },
              buttonLabel),
              React.createElement(  
                "button",
                {
                  class: "btn btn-clear  opblock-control__btn",
                  onClick: clearMessages
                },
                "Clear"));
        } else {
          buttonGroup = 
            React.createElement(
              "div",
              { class: "execute-wrapper"}, 
              React.createElement(
                "button",
                {
                  class: "btn execute opblock-control__btn",
                  onClick: isConnected ? disconnectHub : connectHub
                },
                buttonLabel));
        }

        const { getSampleSchema } = system.fn;
        const highlightCodeComponent = system.getComponent("HighlightCode", true);
        const schema = props.responses.getIn(["200", "content", "application/json", "schema"]);

        const sampleResponse = getSampleSchema(
          schema,
          "application/json",
          undefined,
          undefined);

        const getExampleComponent = ( sampleResponse, highlightCodeComponent ) => {
          if (sampleResponse == null) {
            return null;
          }

          return React.createElement("div", {}, React.createElement(highlightCodeComponent, {className:"example", language:"json"}, _stringify(sampleResponse)));
        }

        const example = getExampleComponent( sampleResponse, highlightCodeComponent );

        if (schema || example) {
          const modelExampleComponent = system.getComponent("modelExample");

          const modelExample = React.createElement(modelExampleComponent, {
            getComponent: props.getComponent,
            getConfigs: props.getConfigs,
            specSelectors: specSelectors,
            specPath: props.specPath.push("schema"),
            schema: schema,
            example: example
          });
          
          const exampleRow = React.createElement(
            "tr",
            {},
            React.createElement("td", {class: "response-col_event", style: {width: "100%"}, colspan: 2}, 
              modelExample
            )
          );

          rows.push(exampleRow);
        }

        const table = React.createElement(
          "table",
          {
            class: "responses-table",
            "aria-live": "polite",
            role: "region",
          },
          React.createElement("tbody", {}, ...rows));

        const responsesSection = React.createElement("div", {class: "responses-wrapper"},
          React.createElement("div", {class: "opblock-section-header"},
            React.createElement("div", {class: "tab-header"},
              React.createElement("div", {class: "tab-item active"},
                React.createElement("h4", {class: "opblock-title"}, 
                  React.createElement("span", {},
                    "Messages"
                  )
                )
              )
            ),
          ),
          React.createElement("div", {class: "responses-inner"}, table)
        );

        if (method === "get"  ) {
          return React.createElement("div", {style: {"margin-top": "-40px"}}, [buttonGroup, responsesSection]);
        } else {
          return React.createElement("div", {}, [responsesSection]);
        }
      },
      Message: ({message, type}) => {
        const {React, getComponent, getConfigs} = system;
        const ResponseBody = getComponent("responseBody");

        const body = React.createElement(ResponseBody, {
          content: JSON.stringify(message),
          contentType: "application/json",
          getConfigs,
          getComponent
        });

        const background = type === "send" ? "#ffa500" : "#49cc90";
        return React.createElement("tr", {},
          React.createElement("td", {class: "response-col_event"},
            React.createElement("span", {
                class: "opblock-summary-method",
                style: {minWidth: 90, display: "inline-block", background}
              },
              type.toUpperCase()),
          ),
          React.createElement("td", {class: "response-col_description"}, body)
        );
      }
    },

    wrapComponents: {
      // Wrap the execute button so we can change the text for streams to show "Start" or "Stop"
      // and intercept the click action to handle stopping a stream.  We otherwise use the default
      // action so we can invoke normal calls to the hub (including calls to start a stream) to
      // use the default parameter validation etc.
      execute: (Original, system) => (props) => {
        const { React, hubSelectors, hubActions } = system;
        const { path, method, disabled } = props;

        // Use the original button for anything that isnt SignalR stream
        if (path.indexOf('!') < 0 || method !== "put") {
          return React.createElement(Original, props);
        }

        // Determine if we currently have a stream subscription running for the path
        // and then display the appropriate "Start" or "Stop" action
        const subscription = hubSelectors.getStreamSubscription(path);

        if (subscription) {
          const action = () => {
            subscription.dispose();
            hubActions.setStreamSubscription({path, subscription: undefined});
          };

          return React.createElement(
            "button",
            {
              className: "btn execute opblock-control__btn",
              onClick: action,
              disabled: disabled
            },
            "Stop");
        } else {
          const wrapped = new Original();
          wrapped.props = props;

          return React.createElement(
            "button",
            {
              className: "btn execute opblock-control__btn",
              onClick: wrapped.onClick,
              disabled: disabled
            },
            "Start");
          }
      },
      parameters: (Original, system) => (props) => {
        const {React} = system;

        // Hide the parameters for the SignalR client operations
        const [ path, method ] = props.pathMethod;
        if (path.indexOf("!") >= 0 && method === "get") {
          return null;
        }
        return React.createElement(Original, props);
      },
      OperationSummary: (Original, system) => (props) => {
        const {React} = system;

        // Modify the SignalR operations to use "SEND" and "RECEIVE" method labels
        if (props.operationProps.get("path").indexOf('!') >= 0) {
          const method = props.operationProps.get("method").toLowerCase();

          if (method === "get") {
            props.operationProps = props.operationProps.set("method", "receive");
          } else if (method === "put") {
            props.operationProps = props.operationProps.set("method", "stream");
          } else if (method === "patch") {
            props.operationProps = props.operationProps.set("method", "send stream");
          } else {
            props.operationProps = props.operationProps.set("method", "send");
          }
        }

        return React.createElement(Original, props);
      },
      // Hide the curl command UI for SignalR operations
      curl: (Original, system) => (props) => {
        const {React} = system;
        const url = props.request.get("url");
                
        if (url.indexOf("!") >= 0) {
          return null;
        }

        return React.createElement(Original, props);
      },        
      // Use the custom hub responses component for SignalR operations
      responses: (Original, system) => (props) => {
        const {React, hubActions, hubSelectors, specSelectors} = system;

        if (props.path.indexOf("!") >= 0) {
          const signalRResponsesComponent = system.getComponent("SignalRResponses");
          return React.createElement(
            signalRResponsesComponent, 
            {
              hubActions,
              hubSelectors,
              specSelectors,
              ...props
            }
          );
        }
        return React.createElement(Original, props);
      },
      parameterRow: (Original, system) => (props) => {
        const {React} = system;
        const [_, path, method ] = props.specPath.toJS();

        // Use the original component for anything that isn't SignalR
        if (path.indexOf('!') < 0 ) {
          return React.createElement(Original, props);
        }

        // Render the orginal so we can mutate the resulting DDOM
        const original = new Original(props, system.context);
        const rendered = original.render();

        // Remove the "(query)" text from the rendered DOM
        const length = rendered.props.children[0].props.children.length;
        rendered.props.children[0].props.children[length-1].props.children = [];

        return rendered;
      },
      // Incercept the 'Cancel' button and stop and running stream subscription
      operation: (Original, system) => (props) => {
        const { React, hubSelectors, hubActions } = system;
        const [_, path, method ] = props.specPath.toJS();

        // Use the original button for anything that isn't a SignalR stream
        if (path.indexOf('!') < 0 || method !== "put") {
          return React.createElement(Original, props);
        }

        // Hook the onCancelClick function and use it to stop any active subscription
        // before continuting with the rest of the cancel action.
        const original = props.onCancelClick;
        props.onCancelClick = () => {
          const subscription = hubSelectors.getStreamSubscription(path);
          if (subscription) {
            subscription.dispose();
            hubActions.setStreamSubscription({path, subscription: undefined});
          }
          original();
        }

        return React.createElement(Original, props);
      }
    }
  };
};

// NSwag doesn't provide a mechanism for adding a SwaggerUI plugin so we
// have to hook the existing onload function and inject the plugin by re-creating
// the UI with the NSwag provided config.
const originalOnLoad = window.onload;

window.onload = function() {

  originalOnLoad();

  const config = window.ui.getConfigs();

  config.plugins.push(SignalRPlugin);

  window.ui = SwaggerUIBundle(config);
}