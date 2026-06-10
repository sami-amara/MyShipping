/* eslint-disable no-undef */
// ═══════════════════════════════════════════════════════════════
// SignalR Client with JWT Bearer Token Authentication
// Handles automatic reconnection and token refresh
// ═══════════════════════════════════════════════════════════════
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
(function () {
    'use strict';
    const SignalRClient = {
        connection: null,
        hubUrl: 'https://localhost:7228/hubs/yourHub', // ✅ Add /hubs/ prefix
        isConnecting: false,
        reconnectAttempts: 0,
        maxReconnectAttempts: 5,
        reconnectDelay: 5000,
        // ═══════════════════════════════════════════════════════════
        // Get Access Token from UI
        // ═══════════════════════════════════════════════════════════
        getAccessToken: function () {
            return __awaiter(this, void 0, void 0, function* () {
                try {
                    //console.log('🔍 Fetching AccessToken for SignalR...');
                    const response = yield fetch('/Account/GetAccessToken', {
                        credentials: 'include'
                    });
                    if (!response.ok) {
                        //console.error('❌ Failed to get AccessToken:', response.status);
                        return null;
                    }
                    const data = yield response.json();
                    if (data && data.token) {
                        //console.log('✅ AccessToken retrieved:', data.token.substring(0, 20) + '...');
                        return data.token;
                    }
                    //console.error('❌ No token in response:', data);
                    return null;
                }
                catch (error) {
                    //console.error('❌ Error getting AccessToken:', error);
                    return null;
                }
            });
        },
        // ═══════════════════════════════════════════════════════════
        // Start SignalR Connection
        // ═══════════════════════════════════════════════════════════
        start: function () {
            return __awaiter(this, void 0, void 0, function* () {
                if (this.isConnecting) {
                    console.warn('⚠️ SignalR connection already in progress');
                    return false;
                }
                if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
                    console.log('✅ SignalR already connected');
                    return true;
                }
                try {
                    this.isConnecting = true;
                    console.log('▶️ Starting SignalR connection...');
                    // ✅ Get fresh access token
                    const accessToken = yield this.getAccessToken();
                    if (!accessToken) {
                        console.error('❌ Cannot start SignalR: No access token available');
                        this.isConnecting = false;
                        return false;
                    }
                    // ✅ Create connection with Bearer token
                    this.connection = new signalR.HubConnectionBuilder()
                        .withUrl(this.hubUrl, {
                        accessTokenFactory: () => accessToken, // ✅ Pass JWT token
                        withCredentials: true, // ✅ Send cookies (for refresh token)
                        skipNegotiation: false, // ✅ Use negotiation
                        transport: signalR.HttpTransportType.WebSockets |
                            signalR.HttpTransportType.ServerSentEvents |
                            signalR.HttpTransportType.LongPolling
                    })
                        .configureLogging(signalR.LogLevel.Information)
                        .withAutomaticReconnect({
                        nextRetryDelayInMilliseconds: retryContext => {
                            // Exponential backoff: 0s, 2s, 10s, 30s, then stop
                            if (retryContext.previousRetryCount === 0) {
                                return 0;
                            }
                            if (retryContext.previousRetryCount < 3) {
                                return Math.min(2000 * Math.pow(2, retryContext.previousRetryCount), 30000);
                            }
                            return null; // Stop reconnecting
                        }
                    })
                        .build();
                    // ═══════════════════════════════════════════════════
                    // Connection Event Handlers
                    // ═══════════════════════════════════════════════════
                    this.connection.onreconnecting(error => {
                        console.warn('🔄 SignalR reconnecting...', (error === null || error === void 0 ? void 0 : error.message) || '');
                        this.onReconnecting(error);
                    });
                    this.connection.onreconnected(connectionId => {
                        console.log('✅ SignalR reconnected:', connectionId);
                        this.reconnectAttempts = 0;
                        this.onReconnected(connectionId);
                    });
                    this.connection.onclose((error) => __awaiter(this, void 0, void 0, function* () {
                        console.error('❌ SignalR connection closed:', (error === null || error === void 0 ? void 0 : error.message) || '');
                        this.isConnecting = false;
                        yield this.onClosed(error);
                    }));
                    // ═══════════════════════════════════════════════════
                    // Server Message Handlers
                    // ═══════════════════════════════════════════════════
                    this.setupMessageHandlers();
                    // ✅ Start connection
                    yield this.connection.start();
                    console.log('✅ SignalR connected successfully');
                    this.reconnectAttempts = 0;
                    this.isConnecting = false;
                    // ✅ Optional: Request user info from server
                    yield this.requestWhoAmI();
                    return true;
                }
                catch (error) {
                    /* console.error('❌ SignalR connection error:', error);*/
                    this.isConnecting = false;
                    yield this.handleConnectionError(error);
                    return false;
                }
            });
        },
        // ═══════════════════════════════════════════════════════════
        // Stop Connection
        // ═══════════════════════════════════════════════════════════
        stop: function () {
            return __awaiter(this, void 0, void 0, function* () {
                try {
                    if (this.connection) {
                        console.log('🛑 Stopping SignalR connection...');
                        yield this.connection.stop();
                        this.connection = null;
                        console.log('✅ SignalR connection stopped');
                    }
                }
                catch (error) {
                    console.error('❌ Error stopping SignalR:', error);
                }
            });
        },
        // ═══════════════════════════════════════════════════════════
        // Reconnect with New Token (after token refresh)
        // ═══════════════════════════════════════════════════════════
        reconnectWithNewToken: function () {
            return __awaiter(this, void 0, void 0, function* () {
                try {
                    console.log('🔄 Reconnecting SignalR with new token...');
                    // ✅ Stop existing connection
                    //tesing only
                    yield this.stop();
                    // ✅ Wait a moment before reconnecting
                    yield new Promise(resolve => setTimeout(resolve, 1000));
                    // ✅ Start new connection with fresh token
                    const success = yield this.start();
                    if (success) {
                        console.log('✅ SignalR reconnected with new token');
                    }
                    else {
                        console.error('❌ Failed to reconnect SignalR with new token');
                    }
                    return success;
                }
                catch (error) {
                    console.error('❌ Error reconnecting SignalR:', error);
                    return false;
                }
            });
        },
        // ═══════════════════════════════════════════════════════════
        // Setup Message Handlers (from server)
        // ═══════════════════════════════════════════════════════════
        setupMessageHandlers: function () {
            if (!this.connection)
                return;
            // ✅ WhoAmI response
            this.connection.on('WhoAmI', (data) => {
                console.log('👤 WhoAmI response:', data);
                this.onWhoAmI(data);
            });
        },
        // ═══════════════════════════════════════════════════════════
        // Request User Info from Server
        // ═══════════════════════════════════════════════════════════
        requestWhoAmI: function () {
            return __awaiter(this, void 0, void 0, function* () {
                try {
                    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
                        yield this.connection.invoke('WhoAmI');
                    }
                }
                catch (error) {
                    console.error('❌ Error calling WhoAmI:', error);
                }
            });
        },
        // ═══════════════════════════════════════════════════════════
        // Handle Connection Error
        // ═══════════════════════════════════════════════════════════
        handleConnectionError: function (error) {
            return __awaiter(this, void 0, void 0, function* () {
                const errorMessage = (error === null || error === void 0 ? void 0 : error.message) || (error === null || error === void 0 ? void 0 : error.toString()) || 'Unknown error';
                // ✅ Check if error is 401 Unauthorized (token expired)
                if (errorMessage.includes('401') || errorMessage.includes('Unauthorized')) {
                    console.warn('🔐 SignalR connection unauthorized - attempting token refresh...');
                    // ✅ Try to refresh token
                    if (window.ApiClient && typeof ApiClient.refreshToken === 'function') {
                        const refreshed = yield ApiClient.refreshToken();
                        if (refreshed) {
                            console.log('✅ Token refreshed, reconnecting SignalR...');
                            yield this.reconnectWithNewToken();
                            return;
                        }
                        else {
                            console.error('❌ Token refresh failed');
                        }
                    }
                }
                // ✅ Retry connection with backoff
                if (this.reconnectAttempts < this.maxReconnectAttempts) {
                    this.reconnectAttempts++;
                    const delay = this.reconnectDelay * this.reconnectAttempts;
                    console.log(`🔄 Retrying SignalR connection in ${delay / 1000}s (attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts})...`);
                    setTimeout(() => {
                        this.start();
                    }, delay);
                }
                else {
                    console.error('❌ Max reconnection attempts reached. Giving up.');
                    this.onMaxReconnectAttemptsReached();
                }
            });
        },
        // ═══════════════════════════════════════════════════════════
        // Event Callbacks (Override these in your app)
        // ═══════════════════════════════════════════════════════════
        onReconnecting: function (error) {
            // ✅ Override this to show UI indicator
            console.log('🔄 Connection lost, reconnecting...');
        },
        onReconnected: function (connectionId) {
            // ✅ Override this to hide UI indicator
            console.log('✅ Reconnected to server');
        },
        onClosed: function (error) {
            return __awaiter(this, void 0, void 0, function* () {
                // ✅ Override this to handle disconnection
                console.log('❌ Connection closed');
                yield this.handleConnectionError(error);
            });
        },
        onWhoAmI: function (data) {
            // ✅ Override this to handle user info
            if (data && data.Authenticated) {
                console.log('✅ Authenticated as:', data.User);
            }
            else {
                console.warn('⚠️ Not authenticated');
            }
        },
        onMaxReconnectAttemptsReached: function () {
            // ⚠️ FIX: Removed automatic page reload.
            // The original reload was intended as a recovery mechanism, but it caused
            // multi-step forms (e.g. Create Shipment) to be completely wiped mid-entry
            // whenever SignalR failed to reconnect — destroying all user-entered data.
            // Silently giving up is the safe choice: pages that need live updates simply
            // won't receive them until the user refreshes manually.
            console.warn('⚠️ SignalR: max reconnect attempts reached. Live updates paused — no page reload.');
            // 🗑️ COMMENTED OUT — was reloading the page and wiping active forms:
            // console.error('❌ Could not reconnect to server. Reloading page...');
            // try {
            //     const reloadFlagKey = 'signalr-auto-reload-once';
            //     const alreadyReloaded = sessionStorage.getItem(reloadFlagKey) === '1';
            //
            //     if (!alreadyReloaded) {
            //         sessionStorage.setItem(reloadFlagKey, '1');
            //         setTimeout(() => {
            //             window.location.reload();   // ← was destroying the Create form
            //         }, 300);
            //         return;
            //     }
            //
            //     sessionStorage.removeItem(reloadFlagKey);
            // } catch (e) {
            //     window.location.reload();           // ← same problem in the catch branch
            // }
        },
        // ═══════════════════════════════════════════════════════════
        // Send Message to Server
        // ═══════════════════════════════════════════════════════════
        invoke: function (methodName, ...args) {
            return __awaiter(this, void 0, void 0, function* () {
                try {
                    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
                        console.warn('⚠️ SignalR not connected, attempting to connect...');
                        yield this.start();
                    }
                    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
                        return yield this.connection.invoke(methodName, ...args);
                    }
                    else {
                        throw new Error('SignalR connection not available');
                    }
                }
                catch (error) {
                    console.error(`❌ Error invoking ${methodName}:`, error);
                    throw error;
                }
            });
        },
        // ═══════════════════════════════════════════════════════════
        // Check Connection State
        // ═══════════════════════════════════════════════════════════
        isConnected: function () {
            return this.connection && this.connection.state === signalR.HubConnectionState.Connected;
        },
        getState: function () {
            if (!this.connection)
                return 'Disconnected';
            switch (this.connection.state) {
                case signalR.HubConnectionState.Connecting: return 'Connecting';
                case signalR.HubConnectionState.Connected: return 'Connected';
                case signalR.HubConnectionState.Reconnecting: return 'Reconnecting';
                case signalR.HubConnectionState.Disconnected: return 'Disconnected';
                default: return 'Unknown';
            }
        }
    };
    // ═══════════════════════════════════════════════════════════════
    // Auto-start on Page Load
    // ═══════════════════════════════════════════════════════════════
    document.addEventListener('DOMContentLoaded', function () {
        // ⚠️ FIX: Guard against auto-starting SignalR on pages that contain a
        // multi-step shipment creation form (#createShipmentForm).
        //
        // WHY: signalr-client.js is loaded in the public _Layout so it runs on
        // EVERY page. On the Create Shipment page the form can take several minutes
        // to fill. If SignalR fails to connect and exhausts its retry attempts it
        // previously called window.location.reload() — wiping all entered data.
        // Even with that reload now removed, opening an unnecessary hub connection
        // on a data-entry page wastes resources and can trigger confusing 401 retries.
        //
        // SAFE FOR ADMIN LIST: Pages that need real-time updates (e.g. admin list)
        // load AdminListShipmentsSignalR.js which calls SignalRClient.start()
        // explicitly — so those pages are completely unaffected by this guard.
        if (document.querySelector('#createShipmentForm')) {
            console.log('📡 SignalR auto-start skipped — multi-step form detected on this page.');
            return;
        }
        console.log('📡 Initializing SignalR client...');
        SignalRClient.start();
        // 🗑️ COMMENTED OUT — original unconditional auto-start (kept for reference):
        // console.log('📡 Initializing SignalR client...');
        // SignalRClient.start();
    });
    // ═══════════════════════════════════════════════════════════════
    // Expose globally
    // ═══════════════════════════════════════════════════════════════
    window.SignalRClient = SignalRClient;
    // ✅ Alias for backward compatibility
    window.reconnectSignalR = () => SignalRClient.reconnectWithNewToken();
})();
//# sourceMappingURL=signalr-client.js.map