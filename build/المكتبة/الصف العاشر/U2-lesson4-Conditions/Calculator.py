#Conditions and math. operator
num1 = float(input('Enter First Number: '))
num2 = float(input('Enter Second Number: '))
operator = input('Enter Math. Operator(+, -, *, /): ')
if operator == '+':
    print(num1+ num2)
                                 #Check the math. operator = -
    print (num1-num2)
                                 #Check the math. operator = *
    print(num1 * num2)
elif operator == '/':
    if num2 != 0:

    else:

else:
    print("Invalid operator")