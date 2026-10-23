// FastEndpoints keeps its serializer options and service resolver in process-wide statics that each host
// overwrites at start-up, so no host of this suite may start while another one serves a request.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
