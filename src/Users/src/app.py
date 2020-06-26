from flask import Flask, jsonify, make_response, request
from flask_cors import CORS, cross_origin
from pymongo import MongoClient
from redis import Redis
import os
import socket

# Development and production behavior might vary; decide runtime
is_development = os.environ.get('TODOAPP_IsDevelopment') is not None

# Get connection string from environment variable
mongo_url = os.getenv('TODOAPP_MongoUrl', 'mongodb://mongodb:27017')
redis_url = os.getenv('TODOAPP_RedisUrl', 'redis')

mongo = MongoClient(mongo_url)
redis = Redis(host=redis_url, port=6379)

app = Flask(__name__)

if is_development:
    # Allow CORS; makes frontend development easier; must not be used in production
    cors = CORS(app)
    app.config['CORS_HEADERS'] = 'Content-Type'

# List all users
@app.route("/api/users", methods=['GET'])
@cross_origin()
def get_all_users():
    users_collection = mongo.todoapp.users
    output = []
    for u in users_collection.find():
        output.append({'name': u['name'], 'id': u['id']})
    return jsonify(output)


# Get a particular user
@app.route("/api/users/<int:id>", methods=['GET'])
@cross_origin()
def get_user(id):
    users_collection = mongo.todoapp.users
    u = users_collection.find_one({'id': id})
    if u:
        output = {'name': u['name'], 'id': u['id']}
        return jsonify(output)
    else:
        return make_response(jsonify({'error': 'Not found'}), 404)

# Delete a particular user
@app.route("/api/users/<int:id>", methods=['DELETE'])
@cross_origin()
def delete_user(id):
    users_collection = mongo.todoapp.users
    result = users_collection.delete_one({'id': id})
    if result.deleted_count > 0:
        redis.sadd('DeleteUserQueue', str(id))
        return make_response('OK', 200)
    else:
        return make_response(jsonify({'error': 'Not found'}), 404)


# Decide if the caller is authenticated; used by forward authentication
@app.route("/api/auth", methods=['GET'])
def get_is_auth():
    # TODO 7. feladat
    # return make_response(jsonify({'error': 'Unauthorized'}), 401)
    return make_response(jsonify({'auth': 'ok'}), 200)


if __name__ == "__main__":

    # Add sample data
    if is_development:
        users_collection = mongo.todoapp.users
        users_collection.replace_one(
            {'id': 1}, {'id': 1, 'name': 'Alma'}, True)
        users_collection.replace_one(
            {'id': 2}, {'id': 2, 'name': 'Banan'}, True)
        users_collection.replace_one(
            {'id': 3}, {'id': 3, 'name': 'Citrom'}, True)

    # Start the Flash host
    app.run(host='0.0.0.0', port=80)
