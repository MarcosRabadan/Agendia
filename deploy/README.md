# deploy/ — Infraestructura local de desarrollo

Contiene la infra que Agendia necesita para desarrollo/pruebas en local.
**No es infraestructura de produccion.**

## Contenido

- `docker-compose.yml` — levanta dos servicios en contenedores Docker:
  - **Seq** (visor de logs) → http://localhost:5341
  - **RabbitMQ** (broker de mensajeria, con panel de administracion) → http://localhost:15672
    (usuario `agendia`, clave `agendia`)

## Requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado y en marcha.

## Uso

Desde esta carpeta (`deploy/`):

```bash
docker compose up -d      # arranca en segundo plano (la 1a vez descarga las imagenes)
docker compose ps         # ver estado
docker compose logs -f rabbitmq   # ver logs en vivo de un servicio
docker compose down       # parar (los datos se conservan en los volumenes)
```

## Notas

- Las versiones de las imagenes estan **fijadas** (no `latest`) para que la infra sea
  reproducible. Actualiza los tags conscientemente.
- Los datos persisten en volumenes Docker (`seq-data`, `rabbitmq-data`), asi que
  parar/arrancar no pierde la configuracion ni las colas.
- RabbitMQ aun no esta conectado a Agendia (el transporte de eventos es log-only por
  ahora, ver `docs/events-contract.md`). Se conectara cuando se elija el broker del sistema.
