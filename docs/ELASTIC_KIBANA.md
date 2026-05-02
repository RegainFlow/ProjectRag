# Local Elasticsearch And Kibana

This project uses Elasticsearch as the Phase 4 hybrid retrieval backend. SQLite remains the metadata database; Elasticsearch stores searchable chunk text, metadata, and embeddings.

The commands below run an insecure local development setup. Do not use this configuration for production or shared environments.

## Start Elasticsearch

Create a Docker network:

```bash
docker network create elastic
```

Pull Elasticsearch:

```bash
docker pull docker.elastic.co/elasticsearch/elasticsearch:9.3.4
```

Start a single-node cluster:

```bash
docker run --name es01 \
  --net elastic \
  -e "discovery.type=single-node" \
  -e "xpack.security.enabled=false" \
  -e "xpack.security.http.ssl.enabled=false" \
  -e "xpack.security.transport.ssl.enabled=false" \
  -p 9200:9200 \
  -m 1GB \
  docker.elastic.co/elasticsearch/elasticsearch:9.3.4
```

Verify Elasticsearch is running:

```bash
curl http://localhost:9200
```

Check cluster health:

```bash
curl "http://localhost:9200/_cluster/health?pretty"
```

For this insecure local setup, skip password reset, enrollment token, and certificate commands. Those are for secured Elasticsearch/Kibana setups.

## Start Kibana

Pull Kibana:

```bash
docker pull docker.elastic.co/kibana/kibana:9.3.4
```

Start Kibana:

```bash
docker run --name kib01 \
  --net elastic \
  -e "ELASTICSEARCH_HOSTS=http://es01:9200" \
  -e "XPACK_SECURITY_ENABLED=false" \
  -p 5601:5601 \
  docker.elastic.co/kibana/kibana:9.3.4
```

Open Kibana:

```text
http://localhost:5601
```

## Useful Local Commands

Delete the ProjectRag search index after mapping changes:

```bash
curl -X DELETE "http://localhost:9200/projectrag-chunks"
```

Count indexed chunks:

```bash
curl "http://localhost:9200/projectrag-chunks/_count"
```

Ingest sample docs with a longer client timeout:

```bash
curl -X POST "http://localhost:5260/api/v1/ingestions" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  --max-time 300 \
  -d '{"sourcePath":"samples/docs"}'
```

Ask with a longer client timeout:

```bash
curl -X POST "http://localhost:5260/api/v1/ask" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  --max-time 300 \
  -d '{"question":"What are the late payment fees?","topK":5}'
```

## Cleanup

Stop containers:

```bash
docker stop kib01 es01
```

Remove containers:

```bash
docker rm kib01 es01
```

Remove the Docker network:

```bash
docker network rm elastic
```
