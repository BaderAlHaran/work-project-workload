import sqlite3
connection = sqlite3.connect('Kuwait_landmarks.db')
cursor = connection.cursor()









connection.commit()
connection.close()