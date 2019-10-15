# Kubernetes telepítéshez használt erőforrás leírók

## Adatbázisok

- Redis: Deployment-ként telepítjük és nem csatolunk hozza diszket
- MongoDB: StatefulSet-ként telepítjük, de nem csatolunk hozzá diszket (környezeti probléma miatt nehéz lenne)
- Elasticsearch: StatefulSet-ként telepítjük és a perzisztens adattároláshoz dinamikus PersistentVolumeClaim-et használunk

## Todoapp alkalmazás komponensek

- Minden Deployment-ként települ.
- Mindenhez tartozik Service és Ingress.
