# Program to calculate BMI with error handling
while True:
            :
        # Get user input: weight and height
        weight_kg = float(input('Enter your weight in kilograms: '))
        height_m = float(input('Enter your height in meters: '))

        # Calculate BMI
        bmi_value = weight_kg / (height_m ** 2)

        # Display the BMI
        print(f'Your BMI is: {bmi_value:.2f}')

        # Determine BMI category
        if bmi_value < 18.5:
            print('You are underweight.')
        elif bmi_value >= 18.5 and bmi_value < 25:
            print('You have a normal weight.')

            print('You are overweight.')
        else:
            print('You are obese.')
        stop_program = input('Press Enter to continue or Q to quit.').capitalize()
        if stop_program == 'Q':
            break
    
        print('Error: Please enter valid numeric values for weight and height.')

        print('Error: You cannot divide by zero!')