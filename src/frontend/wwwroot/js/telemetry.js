window.WateringTelemetry = {
  _tracer: null,
  _fallbackEndpoint: "/api/otel/client-event",
  start: async function (config) {
    if (!config) {
      return;
    }

    if (config.fallbackEndpoint) {
      window.WateringTelemetry._fallbackEndpoint = config.fallbackEndpoint;
    }

    if (!config.enabled) {
      return;
    }

    if (!config.useBrowserSdk) {
      return;
    }

    try {
      const [
        sdkTraceWeb,
        exporterOtlpHttp,
        resourcesPkg,
        semconvPkg,
        instrumentationCore,
        docLoadInstrPkg,
        fetchInstrPkg
      ] = await Promise.all([
        import("https://esm.sh/@opentelemetry/sdk-trace-web@1.24.1"),
        import("https://esm.sh/@opentelemetry/exporter-trace-otlp-http@0.51.1"),
        import("https://esm.sh/@opentelemetry/resources@1.24.1"),
        import("https://esm.sh/@opentelemetry/semantic-conventions@1.24.1"),
        import("https://esm.sh/@opentelemetry/instrumentation@0.51.1"),
        import("https://esm.sh/@opentelemetry/instrumentation-document-load@0.51.1"),
        import("https://esm.sh/@opentelemetry/instrumentation-fetch@0.51.1")
      ]);

      const serviceName = config.serviceName || "WateringController.Frontend";
      const endpointBase = (config.otlpHttpEndpoint || "http://localhost:4318").replace(/\/$/, "");

      const resource = new resourcesPkg.Resource({
        [semconvPkg.SemanticResourceAttributes.SERVICE_NAME]: serviceName
      });

      const provider = new sdkTraceWeb.WebTracerProvider({ resource });
      const exporter = new exporterOtlpHttp.OTLPTraceExporter({
        url: endpointBase + "/v1/traces"
      });

      provider.addSpanProcessor(new sdkTraceWeb.BatchSpanProcessor(exporter));
      provider.register();

      instrumentationCore.registerInstrumentations({
        instrumentations: [
          new docLoadInstrPkg.DocumentLoadInstrumentation(),
          new fetchInstrPkg.FetchInstrumentation({
            propagateTraceHeaderCorsUrls: [/.*/]
          })
        ]
      });

      const tracer = provider.getTracer(serviceName);
      window.WateringTelemetry._tracer = tracer;

      const startupSpan = tracer.startSpan("frontend.startup");
      startupSpan.end();
    } catch (error) {
      console.info("OpenTelemetry browser SDK unavailable; using relay mode.", error);
      window.WateringTelemetry._postFallback("frontend.otel.bootstrap_failed", {
        error: String(error)
      });
    }
  },

  trackEvent: function (name, attributes) {
    try {
      const tracer = window.WateringTelemetry._tracer;
      window.WateringTelemetry._postFallback(name, attributes);
      if (!tracer || !name) {
        return;
      }

      const span = tracer.startSpan(name);
      if (attributes && typeof attributes === "object") {
        Object.entries(attributes).forEach(([key, value]) => {
          if (value === null || value === undefined) {
            return;
          }
          if (typeof value === "string" || typeof value === "number" || typeof value === "boolean") {
            span.setAttribute(key, value);
          } else {
            span.setAttribute(key, JSON.stringify(value));
          }
        });
      }
      span.end();
    } catch (error) {
      console.warn("OpenTelemetry trackEvent failed.", error);
      window.WateringTelemetry._postFallback("frontend.otel.track_failed", {
        error: String(error),
        eventName: name || "unknown"
      });
    }
  },

  _postFallback: function (name, attributes) {
    try {
      const payload = {
        name: name || "unknown",
        attributes: attributes || {},
        sentAt: new Date().toISOString()
      };
      fetch(window.WateringTelemetry._fallbackEndpoint, {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        keepalive: true,
        body: JSON.stringify(payload)
      }).catch(() => {});
    } catch {
      // ignore
    }
  }
};
