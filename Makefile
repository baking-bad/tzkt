prepare:
	docker build -t tzkt-snapshot-dl -f Dockerfile-snapshot .
	docker run --name tzkt-snapshot tzkt-snapshot-dl
	docker cp tzkt-snapshot:/tzkt_db.backup .
	docker rm tzkt-snapshot
	docker rmi tzkt-snapshot-dl
	docker-compose up -d db
	docker-compose exec -T db pg_restore -C --clean --if-exists -v --no-acl --no-owner -U tzkt -d tzkt_db < tzkt_db.backup
	rm tzkt_db.backup
	docker-compose build

start:
	docker-compose up -d

stop:
	docker-compose down
