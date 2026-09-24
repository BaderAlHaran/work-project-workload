import sqlite3


def clear_databases():
    # --- 1. التعامل مع قاعدة بيانات الكتب ---
    print("جاري تهيئة قاعدة بيانات grade11_books...")
    conn1 = sqlite3.connect(r'..\..\grade11_books.db')
    cursor1 = conn1.cursor()

    # حذف الجداول لترك القاعدة فارغة
    cursor1.execute('DROP TABLE IF EXISTS books')
    cursor1.execute('DROP TABLE IF EXISTS publishers')

    conn1.commit()
    conn1.close()
    print("تم حذف جداول قاعدة بيانات الكتب بنجاح.")

    print("-" * 30)

    # --- 2. التعامل مع قاعدة بيانات المعالم ---
    print("جاري تهيئة قاعدة بيانات Kuwait_landmarks...")
    conn2 = sqlite3.connect(r'..\Kuwait_landmarks.db')
    cursor2 = conn2.cursor()

    # حذف الجداول لترك القاعدة فارغة
    cursor2.execute('DROP TABLE IF EXISTS landmarks')
    cursor2.execute('DROP TABLE IF EXISTS events')

    conn2.commit()
    conn2.close()
    print("تم حذف جداول قاعدة بيانات المعالم بنجاح.")


if __name__ == "__main__":
    clear_databases()
    print("\nتمت عملية تصفير قواعد البيانات بالكامل!")