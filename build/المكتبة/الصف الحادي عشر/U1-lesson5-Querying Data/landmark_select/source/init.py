import sqlite3

def run_database_setup():
    # --- 1. إعداد قاعدة بيانات الكتب (grade11_books.db) ---
    # نستخدم r قبل المسار لتجنب مشكلة الـ SyntaxWarning مع علامة \
    path_books = r'../../grade11_books.db'
    conn1 = sqlite3.connect(path_books)
    cursor1 = conn1.cursor()

    # حذف الجداول السابقة للبدء من جديد
    cursor1.execute('DROP TABLE IF EXISTS books')

    # ترتيب category قبل year وحذف cover
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


    # بيانات الكتب بالترتيب الجديد (category ثم year)
    books_data = [
        (1, 'Arabic', 'Arabic SD', 'Art/Science', 2001, 1.5),
        (2, 'French', 'French SD', 'Art', 2007, 1.5),
        (3, 'English', 'English SD', 'Art/Science', 2009, 1.5),
        (4, 'Mathematics', 'Mathematics SD', 'Science', 2013, 1.5),
        (5, 'Mathematics', 'Mathematics SD', 'Art', 2013, 1.5),
        (6, 'Principles of Geography and Economics', 'Social SD', 'Art', 2016, 1.5),
        (7, 'Psychology and sociology', 'Social SD', 'Art', 2016, 1.5),
        (8, 'Islamic History', 'Social SD', 'Art', 2007, 1.5),
        (9, 'Chemistry', 'Science SD', 'Science', 2013, 1.5),
        (10, 'Physics', 'Science SD', 'Science', 2013, 0.0),
        (11, 'Biology', 'Science SD', 'Science', 2013, 1.5),
        (12, 'Geology', 'Science SD', 'Science', 2013, 1.5),
        (13, 'ICT', 'ICT SD', 'Art/Science', 2025, 1.5),
        (14, 'Quran', 'Islamic SD', 'Art/Science', 2003, 1.5),
        (15, 'Islamic Studies', 'Islamic SD', 'Art/Science', 2013, 1.5)
    ]

    cursor1.executemany('INSERT INTO books VALUES (?,?,?,?,?,?)', books_data)
    conn1.commit()
    conn1.close()
    print("تم تحديث قاعدة بيانات الكتب: grade11_books.db")

    # --- 2. إعداد قاعدة بيانات المعالم (Kuwait_landmarks.db) ---
    path_landmarks = r'../Kuwait_landmarks.db'
    conn2 = sqlite3.connect(path_landmarks)
    cursor2 = conn2.cursor()

    cursor2.execute('DROP TABLE IF EXISTS landmarks')

    # إنشاء جدول المعالم بدون AUTOINCREMENT
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


    # بيانات المعالم المستخرجة من المصدر
    landmarks_data = [
        (1, 'Al-Tahrir Tower', 'Central Kuwait City', 'Architecture', 1996, 0.0),
        (2, 'Kuwait National Museum', 'Al-Murqab Area', 'Historical', 1983, 2.5),
        (3, 'The Avenues Mall', 'Al-Rai Area', 'Shopping', 2007, 0.0),
        (4, 'Failaka Island', 'Off the coast', 'Historical', 1950, 5.0),
        (5, 'Souq Al-Mubarakiya', 'Central Kuwait City', 'Traditional', 1897, 0.0),
        (6, 'Sheikh Jaber Al-Ahmad Bridge', 'Kuwait City', 'Modern', 2019, 0.0),
        (7, 'Kuwait National Library', 'Al-Murqab Area', 'Cultural', 1994, 0.0),
        (8, 'Museum of Modern Art', 'Salmiya Area', 'Art', 1980, 1.0),
        (9, 'Cultural Center', 'Kuwait City', 'Cultural', 2016, 0.0),
        (10, 'Grand Mosque of Kuwait', 'Kuwait City', 'Religious', 1986, 0.0,)
    ]


    cursor2.executemany('INSERT INTO landmarks VALUES (?,?,?,?,?,?)', landmarks_data)
    conn2.commit()
    conn2.close()
    print("تم تحديث قاعدة بيانات المعالم: Kuwait_landmarks.db")

if __name__ == "__main__":
    run_database_setup()
    print("\nاكتملت جميع العمليات بنجاح.")