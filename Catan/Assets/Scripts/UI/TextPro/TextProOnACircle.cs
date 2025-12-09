//MIT License

//Copyright(c) 2019 Antony Vitillo(a.k.a. "Skarredghost")

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using TMPro;
using UnityEngine;

namespace UI.TextPro
{
    /// <summary>
    /// Class for drawing a Text Pro text with vertical bending at the start and end
    /// </summary>
    [ExecuteInEditMode]
    public class TextProBend : UI.TextProBend
    {
        /// <summary>
        /// The amount of vertical bending applied to the text.
        /// Positive values bend the start and end upward, negative values bend them downward.
        /// </summary>
        [SerializeField]
        [Tooltip("The amount of vertical bending. Positive = up, Negative = down")]
        private float m_bendAmount = 50.0f;

        /// <summary>
        /// The curve shape for the bend. 0 = linear, 1 = smooth (sine wave), 2 = parabolic
        /// </summary>
        [SerializeField]
        [Tooltip("Bend curve type: 0=Linear, 1=Smooth(Sine), 2=Parabolic")]
        [Range(0, 2)]
        private int m_bendCurveType = 1;

        /// <summary>
        /// Previous value of <see cref="m_bendAmount"/>
        /// </summary>
        private float m_oldBendAmount = float.MaxValue;

        /// <summary>
        /// Previous value of <see cref="m_bendCurveType"/>
        /// </summary>
        private int m_oldBendCurveType = -1;

        /// <summary>
        /// Method executed at every frame that checks if some parameters have been changed
        /// </summary>
        /// <returns></returns>
        protected override bool ParametersHaveChanged()
        {
            //check if parameters have changed and update the old values for next frame iteration
            bool retVal = m_bendAmount != m_oldBendAmount || m_bendCurveType != m_oldBendCurveType;

            m_oldBendAmount = m_bendAmount;
            m_oldBendCurveType = m_bendCurveType;

            return retVal;
        }

        /// <summary>
        /// Computes the transformation matrix that maps the offsets of the vertices of each single character from
        /// the character's center to the final destinations of the vertices so that the text bends vertically
        /// </summary>
        /// <param name="charMidBaselinePos">Position of the central point of the character</param>
        /// <param name="zeroToOnePos">Horizontal position of the character relative to the bounds of the box, in a range [0, 1]</param>
        /// <param name="textInfo">Information on the text that we are showing</param>
        /// <param name="charIdx">Index of the character we have to compute the transformation for</param>
        /// <returns>Transformation matrix to be applied to all vertices of the text</returns>
        protected override Matrix4x4 ComputeTransformationMatrix(Vector3 charMidBaselinePos, float zeroToOnePos, TMP_TextInfo textInfo, int charIdx)      
        {
            // Convert position from [0, 1] to [-0.5, 0.5] range for symmetrical bending
            float normalizedPos = zeroToOnePos - 0.5f;
            
            // Calculate the bend factor based on the curve type
            float bendFactor = 0f;
            
            switch (m_bendCurveType)
            {
                case 0: // Linear
                    bendFactor = Mathf.Abs(normalizedPos) * 2f; // 0 at center, 1 at edges
                    break;
                    
                case 1: // Smooth (Sine wave)
                    // Use cosine for smooth curve: 0 at center, 1 at edges
                    bendFactor = (1f - Mathf.Cos(normalizedPos * 2f * Mathf.PI)) * 0.5f;
                    break;
                    
                case 2: // Parabolic
                    bendFactor = normalizedPos * normalizedPos * 4f; // 0 at center, 1 at edges
                    break;
            }
            
            // Calculate the vertical offset based on bend amount and factor
            float yOffset = m_bendAmount * bendFactor;
            
            // Adjust for multiple lines if needed
            float lineOffset = textInfo.characterInfo[charIdx].lineNumber * textInfo.lineInfo[0].lineHeight;
            
            // Create the new position with the bend applied
            Vector3 newPosition = new Vector3(charMidBaselinePos.x, charMidBaselinePos.y + yOffset - lineOffset, 0);
            
            // Calculate rotation angle based on the slope of the curve (for more natural look)
            float rotationAngle = 0f;
            if (m_bendAmount != 0)
            {
                float derivative = 0f;
                
                switch (m_bendCurveType)
                {
                    case 0: // Linear
                        derivative = normalizedPos > 0 ? 1f : -1f;
                        break;
                        
                    case 1: // Smooth (Sine)
                        derivative = Mathf.Sin(normalizedPos * 2f * Mathf.PI) * Mathf.PI;
                        break;
                        
                    case 2: // Parabolic
                        derivative = normalizedPos * 8f;
                        break;
                }
                
                rotationAngle = -Mathf.Atan(derivative * m_bendAmount / textInfo.characterInfo[charIdx].baseLine) * Mathf.Rad2Deg;
            }
            
            // Return transformation matrix: translate to new position and apply rotation
            return Matrix4x4.TRS(newPosition, Quaternion.AngleAxis(rotationAngle, Vector3.forward), Vector3.one);
        }
    }
}
