from flask import Flask, request, jsonify
from flask_cors import CORS
import mysql.connector

app = Flask(__name__)

CORS(app)

db_host = "localhost"
db_user = "root"
db_password = "your_password"
db_name = "test"

def connect_database():
    connection = mysql.connector.connect(
        host=db_host,
        user=db_user,
        password=db_password,
        database=db_name
    )
    return connection

@app.route("/api/health", methods=["GET"])
def health():
    return jsonify({"status": "ok"})

@app.route("/api/levels", methods=["GET"])
def get_levels():
    connection = connect_database()
    cursor = connection.cursor()
    cursor.execute("SELECT niveauid, niveau FROM niveaus")
    rows = cursor.fetchall()
    cursor.close()
    connection.close()

    levels = []
    for row in rows:
        level = {"level_id": row[0], "name": row[1]}
        levels.append(level)

    return jsonify(levels)

@app.route("/api/levels/<level_id>/questions", methods=["GET"])
def get_questions(level_id):
    connection = connect_database()
    cursor = connection.cursor()

    cursor.execute("SELECT vraagid, vraag FROM vragen WHERE niveauid = %s", (level_id,))
    question_rows = cursor.fetchall()

    questions = []
    for question_row in question_rows:
        question_id = question_row[0]
        question_text = question_row[1]

        cursor.execute("SELECT antwoordenid, antwoord FROM antwoorden WHERE vraagid = %s", (question_id,))
        answer_rows = cursor.fetchall()

        answers = []
        for answer_row in answer_rows:
            answers.append({"answer_id": answer_row[0], "answer": answer_row[1]})

        questions.append({"question_id": question_id, "question": question_text, "answers": answers})

    cursor.close()
    connection.close()
    return jsonify(questions)

@app.route("/api/players", methods=["POST"])
def start_game():
    data = request.get_json()
    name = data["name"]
    level_id = data["level_id"]

    connection = connect_database()
    cursor = connection.cursor()
    cursor.execute("INSERT INTO scores (niveauid, naam) VALUES (%s, %s)", (level_id, name))
    connection.commit()
    score_id = cursor.lastrowid
    cursor.close()
    connection.close()

    print("New player:", name, "score_id:", score_id)
    return jsonify({"score_id": score_id})

@app.route("/api/answers", methods=["POST"])
def save_answer():
    data = request.get_json()
    score_id = data["score_id"]
    answer_id = data["answer_id"]

    connection = connect_database()
    cursor = connection.cursor()

    cursor.execute("""SELECT a.waarde, a.antwoord, v.vraag
                      FROM antwoorden a
                      JOIN vragen v ON v.vraagid = a.vraagid
                      WHERE a.antwoordenid = %s""", (answer_id,))
    answer = cursor.fetchone()
    if answer is None:
        cursor.close()
        connection.close()
        return jsonify({"error": "Answer not found"}), 404

    cursor.execute("INSERT INTO gegeven_antwoorden (scoreid, antwoordenid) VALUES (%s, %s)", (score_id, answer_id))
    cursor.execute("UPDATE scores SET laatste_gespeeld = NOW() WHERE scoreid = %s", (score_id,))
    connection.commit()

    cursor.execute("SELECT score FROM scores WHERE scoreid = %s", (score_id,))
    score = cursor.fetchone()[0]
    cursor.close()
    connection.close()

    if answer[0] > 0:
        correct = True
    else:
        correct = False

    print("---------------------------------")
    print("Player (score_id):", score_id)
    print("Question:", answer[2])
    print("Chosen answer:", answer[1])
    print("Correct:", correct)
    print("Score now:", score)

    return jsonify({
        "message": "Answer saved",
        "correct": correct,
        "score": score
    })

@app.route("/api/scores", methods=["GET"])
def get_scores():
    connection = connect_database()
    cursor = connection.cursor()
    cursor.execute("SELECT naam, laatste_gespeeld, score FROM scores ORDER BY score DESC")
    rows = cursor.fetchall()
    cursor.close()
    connection.close()

    scores = []
    for row in rows:
        scores.append({"name": row[0], "last_played": str(row[1]), "score": row[2]})

    return jsonify(scores)

@app.route("/api/choice", methods=["POST"])
def save_choice():
    data = request.get_json()
    image = data["image"]

    if image not in [1, 2, 3]:
        return jsonify({"error": "Image must be 1, 2 or 3"}), 400

    connection = connect_database()
    cursor = connection.cursor()
    cursor.execute("INSERT INTO image_choices (image) VALUES (%s)", (image,))
    connection.commit()
    choice_id = cursor.lastrowid
    cursor.close()
    connection.close()

    print("Student chose image", image, "(choice_id", str(choice_id) + ")")
    return jsonify({"message": "Choice saved", "choice_id": choice_id, "image": image})

@app.route("/api/choice", methods=["GET"])
def get_latest_choice():
    connection = connect_database()
    cursor = connection.cursor()
    cursor.execute("SELECT choiceid, image FROM image_choices ORDER BY choiceid DESC LIMIT 1")
    row = cursor.fetchone()
    cursor.close()
    connection.close()

    if row is None:
        return jsonify({"choice_id": 0, "image": 0})

    return jsonify({"choice_id": row[0], "image": row[1]})

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, debug=True)
