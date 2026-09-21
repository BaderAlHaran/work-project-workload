import sqlite3

def setup_databases():
    # --- 1. إعداد قاعدة بيانات الكتب (grade11_books.db) ---
    print("جاري معالجة قاعدة بيانات الكتب...")
    conn1 = sqlite3.connect(r'../../grade11_books.db')
    cursor1 = conn1.cursor()

    # حذف الجداول لضبطها من جديد
    cursor1.execute('DROP TABLE IF EXISTS books')

    # إنشاء جدول الكتب (category قبل year وحذف cover)
    cursor1.execute('''
    CREATE TABLE books (
        id INTEGER PRIMARY KEY,
        title TEXT NOT NULL,
        author TEXT,
        category TEXT,
        year INTEGER,
        price REAL
    )
    ''')





    # --- 2. إعداد قاعدة بيانات المعالم (Kuwait_landmarks.db) ---
    print("\nجاري معالجة قاعدة بيانات المعالم...")
    conn2 = sqlite3.connect(r'../Kuwait_landmarks.db')
    cursor2 = conn2.cursor()

    cursor2.execute('DROP TABLE IF EXISTS landmarks')

    # إنشاء جدول المعالم
    cursor2.execute('''
    CREATE TABLE landmarks (
        id INTEGER PRIMARY KEY,
        name TEXT,
        location TEXT,
        category TEXT,
        year INTEGER,
        price REAL
    )
    ''')


if __name__ == "__main__":
    setup_databases()
    print("\nتمت العملية بالكامل!")